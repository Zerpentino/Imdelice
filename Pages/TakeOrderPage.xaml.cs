using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Diagnostics;
using CommunityToolkit.Maui.Views;
using Imdeliceapp;
using Imdeliceapp.Helpers;
using Imdeliceapp.Models;
using Imdeliceapp.Popups;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;

namespace Imdeliceapp.Pages;

public partial class TakeOrderPage : ContentPage
{
    readonly MenusApi _menusApi = new();
    readonly ModifiersApi _modifiersApi = new();
    readonly OrdersApi _ordersApi = new();

    readonly Dictionary<int, List<MenuItemVm>> _itemsByProduct = new();
    readonly Dictionary<int, List<MenuItemVm>> _productVariantsCache = new();
    readonly Dictionary<int, List<ModifierGroupDTO>> _modifierCache = new();
    static readonly ConcurrentDictionary<string, ImageSource> _menuImageCache = new();
    readonly List<MenuSectionVm> _allSections = new();
    readonly List<CartEntry> _cart = new();
    OrderHeaderState _orderHeaderState;

    const string DefaultServiceType = "DINE_IN";
    const string DefaultSource = "POS";

    bool _hasLoaded;
    bool _isInitializingMenu;
    string _searchQuery = string.Empty;

    public ObservableCollection<MenuOptionVm> MenuOptions { get; } = new();
    public ObservableCollection<MenuSectionVm> Sections { get; } = new();
    public ObservableCollection<MenuItemVm> VisibleItems { get; } = new();

    static async Task<string?> GetTokenAsync()
    {
        var s = await SecureStorage.GetAsync("token");
        if (!string.IsNullOrWhiteSpace(s)) return s;
        var p = Preferences.Default.Get("token", string.Empty);
        return string.IsNullOrWhiteSpace(p) ? null : p;
    }

    static HttpClient NewAuthClient(string baseUrl, string token)
    {
        var cli = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(20) };
        cli.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cli;
    }

    MenuOptionVm? _selectedMenu;
    MenuSectionVm? _selectedSection;

    public MenuOptionVm? SelectedMenu
    {
        get => _selectedMenu;
        set
        {
            if (_selectedMenu == value) return;
            _selectedMenu = value;
            OnPropertyChanged();

            if (_selectedMenu != null)
                Preferences.Default.Set("active_menu_id", _selectedMenu.Id);

            if (_isInitializingMenu || value is null)
                return;

            _ = LoadMenuSectionsAsync(value.Id);
        }
    }

    public bool HasMenuOptions => MenuOptions.Count > 0;

    public MenuSectionVm? SelectedSection
    {
        get => _selectedSection;
        private set
        {
            if (_selectedSection == value) return;

            var previous = _selectedSection;
            _selectedSection = value;

            previous?.SetSelected(false);
            _selectedSection?.SetSelected(true);

            OnPropertyChanged();
            UpdateVisibleItems();
        }
    }

    public ICommand ConfigureItemCommand { get; }

    public TakeOrderPage()
    {
        InitializeComponent();
        BindingContext = this;

        MenuOptions.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasMenuOptions));

        ConfigureItemCommand = new Command<MenuItemVm>(async item => await ConfigureItemAsync(item!), item => item != null);

        _orderHeaderState = OrderHeaderState.CreateDefault();

        UpdateCartBadge();
    }

    protected override async void OnAppearing()
{
    base.OnAppearing();

    if (!CanOrder())
    {
        await DisplayAlert("Sin conexión", "Necesitas Internet para ver el menú.", "OK");
        return;
    }

    // Limpia cache de modificadores (por si cambian los grupos/precios)
    _modifierCache.Clear();

    SetLoading(true);
    try
    {
        // Si no hay menú seleccionado, asegura que exista uno
        if (SelectedMenu == null)
            await EnsureMenusLoadedAsync();

        // Con un menú ya elegido, recarga SIEMPRE las secciones al entrar
        if (SelectedMenu != null)
            await LoadMenuSectionsAsync(SelectedMenu.Id, manageLoading: false);
        else
            await DisplayAlert("Menú", "Selecciona un menú para continuar.", "OK");
    }
    finally
    {
        SetLoading(false);
    }
}


    bool CanOrder() => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    async Task<bool> EnsureMenusLoadedAsync()
    {
        if (MenuOptions.Count > 0)
            return true;

        try
        {
            _isInitializingMenu = true;

            var menus = await _menusApi.GetMenusAsync();

            SelectedMenu = null;
            MenuOptions.Clear();
            foreach (var menu in menus
                     .OrderByDescending(m => m.isActive)
                     .ThenByDescending(m => m.publishedAt ?? DateTime.MinValue)
                     .ThenBy(m => m.name, StringComparer.CurrentCultureIgnoreCase))
            {
                MenuOptions.Add(new MenuOptionVm(menu));
            }

            if (MenuOptions.Count == 0)
            {
                await DisplayAlert("Menú", "No hay menús disponibles.", "OK");
                return false;
            }

            var storedId = Preferences.Default.Get("active_menu_id", 0);
            var initial = MenuOptions.FirstOrDefault(m => m.Id == storedId)
                          ?? MenuOptions.FirstOrDefault(m => m.IsActive)
                          ?? MenuOptions.First();

            if (initial != null)
                SelectedMenu = initial;

            return true;
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlert("Menú", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Menú", ex.Message, "OK");
        }
        finally
        {
            _isInitializingMenu = false;
        }

        return false;
    }

    async Task<bool> LoadMenuSectionsAsync(int menuId, bool manageLoading = true)
    {
        if (menuId <= 0)
            return false;

        if (manageLoading)
            SetLoading(true);

        try
        {
            var menu = await _menusApi.GetMenuPublicAsync(menuId);
            if (menu is null || !(menu.sections?.Any() ?? false))
            {
                await DisplayAlert("Menú", "El menú seleccionado no tiene secciones activas.", "OK");
                _allSections.Clear();
                Sections.Clear();
                VisibleItems.Clear();
                return false;
            }

            Title = string.IsNullOrWhiteSpace(menu.name)
                ? (SelectedMenu?.Name ?? "Menú")
                : menu.name;

            _itemsByProduct.Clear();
            _productVariantsCache.Clear();
            _allSections.Clear();

            foreach (var section in menu.sections
                     .Where(s => s.isActive)
                     .OrderBy(s => s.position)
                     .ThenBy(s => s.name, StringComparer.CurrentCultureIgnoreCase))
            {
                var vmSection = new MenuSectionVm(section.id, section.name ?? $"Sección #{section.id}", section.position);
                var sourceItems = section.items
                    .Where(i => i.isActive)
                    .OrderBy(i => i.position)
                    .ThenBy(i => i.displayName, StringComparer.CurrentCultureIgnoreCase)
                    .Select(i => MenuItemVm.From(section, i))
                    .Where(vm => vm != null)
                    .Cast<MenuItemVm>()
                    .ToList();

                await PrefetchImagesAsync(sourceItems);

                if (sourceItems.Any(vm => vm.ImageSource == null))
                {
                    try
                    {
#if DEBUG
                        var detailed = await _menusApi.GetSectionItemsAsync(section.id);
                        var json = JsonSerializer.Serialize(detailed, new JsonSerializerOptions { WriteIndented = true });
                        try { await Clipboard.SetTextAsync(json); } catch { }
                        System.Diagnostics.Debug.WriteLine($"[TakeOrder] Section {section.id} items:\n{json}");
#else
                        var detailed = await _menusApi.GetSectionItemsAsync(section.id);
#endif
                        if (detailed?.Any() ?? false)
                        {
                            sourceItems = detailed
                                .Where(i => i.isActive)
                                .OrderBy(i => i.position)
                                .ThenBy(i => i.displayName, StringComparer.CurrentCultureIgnoreCase)
                                .Select(i => MenuItemVm.From(i, section.name))
                                .Where(vm => vm != null)
                                .Cast<MenuItemVm>()
                                .ToList();
                            await PrefetchImagesAsync(sourceItems);
                        }
                    }
                    catch
                    {
                        // ignorar fallos y quedarse con los datos iniciales
                    }
                }

                await PrefetchImagesAsync(sourceItems);

                foreach (var vm in sourceItems)
                {
                    vmSection.Items.Add(vm);

                    if (vm.ProductId.HasValue)
                    {
                        if (!_itemsByProduct.TryGetValue(vm.ProductId.Value, out var list))
                        {
                            list = new List<MenuItemVm>();
                            _itemsByProduct[vm.ProductId.Value] = list;
                        }
                        list.Add(vm);
                    }
                }

                if (vmSection.Items.Count == 0)
                    continue;

                _allSections.Add(vmSection);
            }

            Sections.Clear();
            foreach (var sec in _allSections)
            {
                sec.RefreshChipColors();
                Sections.Add(sec);
            }

            if (Sections.Count == 0)
            {
                VisibleItems.Clear();
                await DisplayAlert("Menú", "No se encontraron secciones activas.", "OK");
                return false;
            }

            SelectedSection = Sections.FirstOrDefault();
            SectionsView.SelectedItem = SelectedSection;

            UpdateVisibleItems();
            return true;
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlert("Menú", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Menú", ex.Message, "OK");
        }
        finally
        {
            if (manageLoading)
                SetLoading(false);
        }

        return false;
    }

    async Task PrefetchImagesAsync(IEnumerable<MenuItemVm> items)
    {
        var list = items
            .Where(i => i.Raw?.@ref != null)
            .Where(i => i.Raw.@ref?.product?.imageUrl != null || i.Raw.@ref?.variant?.imageUrl != null)
            .ToList();
        if (list.Count == 0) return;

        var token = await GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token)) return;

        var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
        using var http = NewAuthClient(baseUrl, token);

        var sem = new SemaphoreSlim(4);
        var tasks = list.Select(async item =>
        {
            var rawPath = item.Raw.@ref?.variant?.imageUrl ?? item.Raw.@ref?.product?.imageUrl;
            if (string.IsNullOrWhiteSpace(rawPath)) return;

            var path = rawPath!.StartsWith('/') ? rawPath : "/" + rawPath;
            if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                path = "/api" + path;

            var cacheKey = baseUrl + path;
            if (_menuImageCache.TryGetValue(cacheKey, out var cached))
            {
                item.SetImage(cached);
                return;
            }

            await sem.WaitAsync();
            try
            {
                using var resp = await http.GetAsync(path);
                if (!resp.IsSuccessStatusCode)
                    return;

                var bytes = await resp.Content.ReadAsByteArrayAsync();
                var image = ImageSource.FromStream(() => new MemoryStream(bytes));
                _menuImageCache[cacheKey] = image;
                item.SetImage(image);
            }
            finally
            {
                sem.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    void UpdateVisibleItems()
    {
        IEnumerable<MenuItemVm> items = Enumerable.Empty<MenuItemVm>();

        if (!string.IsNullOrWhiteSpace(_searchQuery))
        {
            var q = _searchQuery.Trim().ToLowerInvariant();
            items = _allSections.SelectMany(s => s.Items)
                .Where(i => i.Matches(q));
        }
        else if (SelectedSection != null)
        {
            items = SelectedSection.Items;
        }

        VisibleItems.Clear();
        foreach (var item in items)
            VisibleItems.Add(item);

        if (!VisibleItems.Any())
            SectionsView.SelectedItem = null;
    }

    void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchQuery = e.NewTextValue ?? string.Empty;
        UpdateVisibleItems();
    }

    void SectionsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is MenuSectionVm section)
        {
            SelectedSection = section;
        }
    }

    void SetLoading(bool value)
    {
        LoadingIndicator.IsVisible = LoadingIndicator.IsRunning = value;
        ItemsView.IsVisible = !value;

        MenuPicker.IsEnabled = !value;
        SearchBox.IsEnabled = !value;
        SectionsView.IsEnabled = !value;
        ItemsView.IsEnabled = !value;
    }

    async Task ConfigureItemAsync(MenuItemVm item, CartEntry? editingEntry = null)
    {
        if (item == null)
            return;

#if DEBUG
        var debugInfo = BuildItemDebugInfo(item);
        System.Diagnostics.Debug.WriteLine(debugInfo);
        _ = Clipboard.Default.SetTextAsync(debugInfo);
#endif

        var result = await OpenConfiguratorAsync(item, editingEntry);
        if (result is null)
            return;

        AddOrUpdateCart(result);
    }

    async Task<ConfigureMenuItemResult?> OpenConfiguratorAsync(
        MenuItemVm triggerItem,
        CartEntry? editingEntry = null,
        bool lockQuantity = false,
        int? lockedQuantity = null,
        string? confirmButtonText = null)
    {
        var baseItem = editingEntry?.BaseItem ?? triggerItem;
        var targetProductId = baseItem.ProductId ?? triggerItem.ProductId;

        var relatedVariants = new List<MenuItemVm>();
        if (targetProductId.HasValue && _itemsByProduct.TryGetValue(targetProductId.Value, out var set))
            relatedVariants = set.Where(v => v != null).ToList();

        relatedVariants.Add(baseItem);
        var groupedVariants = relatedVariants
            .GroupBy(v => v.Id)
            .Select(g => g.First())
            .OrderBy(v => v.DisplaySortOrder)
            .ThenBy(v => v.Title)
            .ToList();

        if ((baseItem.IsCombo || triggerItem.IsCombo) && groupedVariants.Count > 0)
        {
            groupedVariants = groupedVariants.Where(v => v.Id == baseItem.Id).ToList();
        }

        relatedVariants = groupedVariants;

        var modifierGroups = new List<ModifierGroupDTO>();
        if (targetProductId.HasValue)
            modifierGroups = await GetModifierGroupsAsync(targetProductId.Value);

        Func<int, Task<IReadOnlyList<VariantModifierGroupLinkDTO>>>? variantRulesLoader = null;
        if (relatedVariants.Any(v => v.VariantId.HasValue) || triggerItem.VariantId.HasValue)
        {
            variantRulesLoader = async variantId =>
            {
                try
                {
                    return await _menusApi.GetVariantModifierGroupsAsync(variantId);
                }
                catch
                {
                    return Array.Empty<VariantModifierGroupLinkDTO>();
                }
            };
        }

        var childConfigs = await BuildComboChildConfigurationsAsync(baseItem, editingEntry);

        var popup = new ConfigureMenuItemPopup(
            baseItem,
            relatedVariants,
            modifierGroups,
            editingEntry,
            variantRulesLoader,
            lockQuantity,
            lockedQuantity,
            confirmButtonText,
            childConfigs);

        var result = await this.ShowPopupAsync(popup) as ConfigureMenuItemResult;
        return result;
    }

    async Task<List<ComboChildConfiguration>> BuildComboChildConfigurationsAsync(
        MenuItemVm baseItem,
        CartEntry? existingEntry)
    {
        var configs = new List<ComboChildConfiguration>();
        if (baseItem?.ComboComponents == null || baseItem.ComboComponents.Count == 0)
            return configs;

        var selections = existingEntry?.ComboChildren?
            .GroupBy(c => (c.ProductId, c.VariantId))
            .ToDictionary(g => g.Key, g => g.First())
            ?? new Dictionary<(int, int?), ComboChildSelection>();

        foreach (var component in baseItem.ComboComponents)
        {
            if (component.ProductId <= 0)
                continue;

            selections.TryGetValue((component.ProductId, component.VariantId), out var existing);
            if (existing == null)
            {
                existing = selections.Values.FirstOrDefault(s => s.ProductId == component.ProductId);
            }

            var variants = await ResolveComboChildVariantsAsync(component);
            IReadOnlyList<MenuItemVm> filteredVariants = variants;
            if (component.VariantId.HasValue)
            {
                var desiredVariantId = component.VariantId.Value;
                var onlyVariant = variants
                    .Where(v => v.VariantId == desiredVariantId)
                    .ToList();

                if (onlyVariant.Count > 0)
                {
                    filteredVariants = onlyVariant;
                }
                else if (variants.Count > 0)
                {
                    filteredVariants = new List<MenuItemVm> { variants[0] };
                }
            }

            var modifierGroups = await GetModifierGroupsAsync(component.ProductId);

            Dictionary<int, IReadOnlyList<VariantModifierGroupLinkDTO>>? variantInlineRules = null;
            foreach (var variant in filteredVariants)
            {
                if (!variant.VariantId.HasValue)
                    continue;
                var inline = variant.Raw?.@ref?.variant?.modifierGroups;
                if (inline == null || inline.Count == 0)
                    continue;
                variantInlineRules ??= new Dictionary<int, IReadOnlyList<VariantModifierGroupLinkDTO>>();
                variantInlineRules[variant.VariantId.Value] = inline;
            }

            IReadOnlyList<VariantModifierGroupLinkDTO>? inlineVariantRules =
                component.VariantReference?.modifierGroups?.Count > 0
                    ? component.VariantReference.modifierGroups
                    : null;

            Func<int, Task<IReadOnlyList<VariantModifierGroupLinkDTO>>>? loader = null;
            if (inlineVariantRules != null)
            {
                loader = _ => Task.FromResult(inlineVariantRules);
            }
            else if (variantInlineRules != null || filteredVariants.Any(v => v.VariantId.HasValue))
            {
                loader = async variantId =>
                {
                    if (variantInlineRules != null && variantInlineRules.TryGetValue(variantId, out var cachedRules))
                        return cachedRules;
                    try
                    {
                        return await _menusApi.GetVariantModifierGroupsAsync(variantId);
                    }
                    catch
                    {
                        return Array.Empty<VariantModifierGroupLinkDTO>();
                    }
                };
            }

            var notesMetadata = ParseComboComponentNotes(component.Notes);
            var allowVariantSelection = notesMetadata.AllowVariantSelection;

            if (!allowVariantSelection && !component.VariantId.HasValue && component.VariantReference == null && filteredVariants.Count > 1)
            {
                var preferred = ResolvePreferredVariantForComponent(component, filteredVariants);
                if (preferred != null)
                    filteredVariants = new List<MenuItemVm> { preferred };
                else
                    filteredVariants = new List<MenuItemVm> { filteredVariants[0] };
            }

            configs.Add(new ComboChildConfiguration(
                component,
                filteredVariants,
                modifierGroups,
                loader,
                existing,
                allowVariantSelection,
                notesMetadata.DisplayNotes));
        }

        return configs;
    }

    async Task<List<MenuItemVm>> ResolveComboChildVariantsAsync(MenuItemVm.ComboComponent component)
    {
        if (component.ProductId <= 0)
            return new List<MenuItemVm>();

        var virtualItems = BuildVirtualMenuItemsFromComponent(component);
        if (component.VariantReference != null && virtualItems.Count > 0)
        {
            await PrefetchImagesAsync(virtualItems);
            return virtualItems;
        }
        var virtualFallback = virtualItems.Count > 0 ? virtualItems : null;

        if (_itemsByProduct.TryGetValue(component.ProductId, out var cached) && cached?.Count > 0)
        {
            return cached
                .GroupBy(v => v.Id)
                .Select(g => g.First())
                .OrderBy(v => v.DisplaySortOrder)
                .ThenBy(v => v.Title)
                .ToList();
        }

        var fallbackVariants = await FetchProductVariantsAsync(component.ProductId);
        if (fallbackVariants.Count > 0)
            return fallbackVariants;

        if (virtualFallback != null)
        {
            await PrefetchImagesAsync(virtualFallback);
            return virtualFallback;
        }

        return new List<MenuItemVm>();
    }

    async Task<List<MenuItemVm>> FetchProductVariantsAsync(int productId)
    {
        if (productId <= 0)
            return new List<MenuItemVm>();

        if (_productVariantsCache.TryGetValue(productId, out var cached) && cached.Count > 0)
            return cached;

        try
        {
            var product = await _menusApi.GetProductAsync(productId);
            if (product == null)
                return new List<MenuItemVm>();

            var section = new MenusApi.MenuPublicSectionDto
            {
                id = -product.id,
                menuId = -1,
                name = product.name ?? $"Producto #{product.id}",
                position = 0,
                isActive = product.isActive,
                items = new List<MenusApi.MenuPublicItemDto>()
            };

            var productRef = new MenusApi.MenuPublicProductReference
            {
                id = product.id,
                name = product.name ?? $"Producto #{product.id}",
                type = product.type,
                description = null,
                priceCents = product.priceCents,
                isActive = product.isActive,
                isAvailable = product.isAvailable ?? true,
                imageUrl = null,
                hasImage = false
            };

            var list = new List<MenuItemVm>();
            if (product.variants != null && product.variants.Count > 0)
            {
                foreach (var variant in product.variants.Where(v => v?.id > 0 && (v.isActive ?? true)))
                {
                    var variantRef = new MenusApi.MenuPublicVariantReference
                    {
                        id = variant.id,
                        name = variant.name,
                        priceCents = variant.priceCents,
                        isActive = variant.isActive ?? true,
                        isAvailable = variant.isAvailable ?? true,
                        product = productRef,
                        imageUrl = variant.imageUrl,
                        hasImage = variant.hasImage ?? false,
                        modifierGroups = variant.modifierGroups ?? new List<VariantModifierGroupLinkDTO>()
                    };

                    var item = new MenusApi.MenuPublicItemDto
                    {
                        id = variant.id,
                        sectionId = section.id,
                        refType = "VARIANT",
                        refId = variant.id,
                        displayName = variant.name ?? product.name,
                        displayPriceCents = variant.priceCents ?? product.priceCents,
                        position = 0,
                        isFeatured = false,
                        isActive = variant.isActive ?? true,
                        @ref = new MenusApi.MenuPublicReferenceDto
                        {
                            kind = "VARIANT",
                            product = productRef,
                            variant = variantRef,
                            components = new List<MenusApi.MenuPublicComboComponent>()
                        }
                    };

                    var vm = MenuItemVm.From(section, item);
                    if (vm != null)
                        list.Add(vm);
                }
            }

            if (list.Count == 0)
            {
                var item = new MenusApi.MenuPublicItemDto
                {
                    id = product.id,
                    sectionId = section.id,
                    refType = "PRODUCT",
                    refId = product.id,
                    displayName = product.name ?? $"Producto #{product.id}",
                    displayPriceCents = product.priceCents,
                    position = 0,
                    isFeatured = false,
                    isActive = product.isActive,
                    @ref = new MenusApi.MenuPublicReferenceDto
                    {
                        kind = "PRODUCT",
                        product = productRef,
                        components = new List<MenusApi.MenuPublicComboComponent>()
                    }
                };

                var vm = MenuItemVm.From(section, item);
                if (vm != null)
                    list.Add(vm);
            }

            await PrefetchImagesAsync(list);
            _productVariantsCache[productId] = list;
            return list;
        }
        catch
        {
            return new List<MenuItemVm>();
        }
    }

    static ComboNotesMetadata ParseComboComponentNotes(string? rawNotes)
    {
        if (string.IsNullOrWhiteSpace(rawNotes))
            return new ComboNotesMetadata(null, false);

        var tokens = new[]
        {
            "[allow-variants]", "[allow-variant]", "[allowvariants]", "{allow-variants}", "{allowvariants}",
            "(allow-variants)", "(allowvariants)", "<allow-variants>", "<allowvariants>", "allow-variants",
            "allowvariants", "[permitir-variantes]", "permitir-variantes"
        };

        var normalized = rawNotes;
        var allow = false;
        foreach (var token in tokens)
        {
            if (normalized.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                allow = true;
                normalized = ReplaceIgnoreCase(normalized, token, string.Empty);
            }
        }

        normalized = normalized
            .Replace("[]", string.Empty)
            .Replace("{}", string.Empty)
            .Replace("()", string.Empty)
            .Replace("<>", string.Empty)
            .Trim('-', '.', ',', ';', ':', ' ')
            .Trim();

        return new ComboNotesMetadata(
            string.IsNullOrWhiteSpace(normalized) ? null : normalized,
            allow);
    }

    static MenuItemVm? ResolvePreferredVariantForComponent(MenuItemVm.ComboComponent component, IReadOnlyList<MenuItemVm> variants)
    {
        if (variants == null || variants.Count == 0)
            return null;

        var expectedPrice = component.ProductReference?.priceCents;
        if (expectedPrice.HasValue)
        {
            var priceMatch = variants.FirstOrDefault(v => GetVariantPriceCents(v) == expectedPrice.Value);
            if (priceMatch != null)
                return priceMatch;
        }

        var preferredName = component.ProductReference?.name;
        if (!string.IsNullOrWhiteSpace(preferredName))
        {
            var normalizedName = NormalizeInvariant(preferredName);
            var nameMatch = variants.FirstOrDefault(v =>
                NormalizeInvariant(v.Raw?.@ref?.variant?.name ?? v.Title) == normalizedName);
            if (nameMatch != null)
                return nameMatch;
        }

        return variants[0];
    }

    static int? GetVariantPriceCents(MenuItemVm item)
    {
        if (item.Raw?.displayPriceCents.HasValue == true)
            return item.Raw.displayPriceCents.Value;
        if (item.Raw?.@ref?.variant?.priceCents.HasValue == true)
            return item.Raw.@ref.variant.priceCents.Value;
        if (item.Raw?.@ref?.product?.priceCents.HasValue == true)
            return item.Raw.@ref.product.priceCents.Value;
        return (int?)Math.Round(item.UnitPrice * 100m, MidpointRounding.AwayFromZero);
    }

    static string ReplaceIgnoreCase(string source, string target, string replacement)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return source ?? string.Empty;
        int index;
        var builder = new StringBuilder();
        int previousIndex = 0;
        while ((index = source.IndexOf(target, previousIndex, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            builder.Append(source, previousIndex, index - previousIndex);
            builder.Append(replacement);
            previousIndex = index + target.Length;
        }
        builder.Append(source, previousIndex, source.Length - previousIndex);
        return builder.ToString();
    }

    static string NormalizeInvariant(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }

    readonly record struct ComboNotesMetadata(string? DisplayNotes, bool AllowVariantSelection);

    List<MenuItemVm> BuildVirtualMenuItemsFromComponent(MenuItemVm.ComboComponent? component)
    {
        var list = new List<MenuItemVm>();
        if (component == null)
            return list;

        var productRef = component.ProductReference ?? component.VariantReference?.product;
        if (productRef == null)
            return list;

        if (component.VariantReference != null)
        {
            list.Add(CreateVirtualMenuItem(productRef, component.VariantReference));
        }
        else
        {
            list.Add(CreateVirtualMenuItem(productRef, null));
        }

        return list;
    }

    static MenuItemVm CreateVirtualMenuItem(
        MenusApi.MenuPublicProductReference productRef,
        MenusApi.MenuPublicVariantReference? variantRef)
    {
        var priceValue = (variantRef?.priceCents ?? productRef.priceCents)?.ToCurrency();
        var title = variantRef?.name ?? productRef.name;
        var subtitle = variantRef?.product?.name ?? productRef.name;

        return new MenuItemVm
        {
            Id = -(variantRef?.id ?? productRef.id),
            SectionId = -1,
            DisplaySortOrder = 0,
            Kind = variantRef != null ? "VARIANT" : "PRODUCT",
            ProductId = productRef.id,
            VariantId = variantRef?.id,
            Title = string.IsNullOrWhiteSpace(title) ? productRef.name : title,
            Subtitle = subtitle,
            Description = productRef.description,
            PriceLabel = priceValue.HasValue
                ? priceValue.Value.ToString("$0.00", CultureInfo.CurrentCulture)
                : "Precio base",
            ReferenceLabel = variantRef != null
                ? $"Variante #{variantRef.id}"
                : $"Producto #{productRef.id}",
            UnitPrice = priceValue ?? 0m,
            Raw = new MenusApi.MenuPublicItemDto
            {
                id = variantRef?.id ?? productRef.id,
                sectionId = -1,
                refType = variantRef != null ? "VARIANT" : "PRODUCT",
                refId = variantRef?.id ?? productRef.id,
                displayName = title,
                displayPriceCents = variantRef?.priceCents ?? productRef.priceCents,
                position = 0,
                isFeatured = false,
                isActive = productRef.isActive,
                @ref = new MenusApi.MenuPublicReferenceDto
                {
                    product = productRef,
                    variant = variantRef
                }
            },
            ComboComponents = Array.Empty<MenuItemVm.ComboComponent>()
        };
    }

#if DEBUG
    static string BuildItemDebugInfo(MenuItemVm item)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== DEBUG: Abrir configurador ===");
        sb.AppendLine($"Item: {item.Title} (Id={item.Id}, ProductId={item.ProductId}, VariantId={item.VariantId})");
        sb.AppendLine($"Tipo: {item.Kind}");
        sb.AppendLine($"Precio: {item.PriceLabel}");
        if (!string.IsNullOrWhiteSpace(item.Description))
            sb.AppendLine($"Descripción: {item.Description}");

        if (item.ComboComponents != null && item.ComboComponents.Count > 0)
        {
            sb.AppendLine("Componentes del combo:");
            foreach (var component in item.ComboComponents)
            {
                var variantLabel = component.VariantName ?? "(sin variante)";
                sb.AppendLine($" - {component.ProductName} · {variantLabel} | Qty={component.Quantity} | Req={component.IsRequired}");
                if (!string.IsNullOrWhiteSpace(component.Notes))
                    sb.AppendLine($"   Notas: {component.Notes}");
            }
        }
        else
        {
            sb.AppendLine("Componentes del combo: ninguno");
        }

        sb.AppendLine("=== FIN DEBUG ===");
        return sb.ToString();
    }
#endif

    async Task<List<ModifierGroupDTO>> GetModifierGroupsAsync(int productId)
    {
        if (_modifierCache.TryGetValue(productId, out var cached))
            return cached;

        try
        {
            var links = await _modifiersApi.GetGroupsByProductAsync(productId);
            var groups = links
                .Where(l => l.group != null && l.group.isActive)
                .OrderBy(l => l.position)
                .Select(l => l.group!)
                .ToList();

            _modifierCache[productId] = groups;
            return groups;
        }
        catch
        {
            return new List<ModifierGroupDTO>();
        }
    }

    void AddOrUpdateCart(ConfigureMenuItemResult result)
    {
        if (result.EditedLineId.HasValue)
        {
            var previous = _cart.FirstOrDefault(c => c.LineId == result.EditedLineId.Value);
            if (previous != null)
                _cart.Remove(previous);
        }

        var existing = _cart.FirstOrDefault(c => c.IsEquivalent(result));
        if (existing != null)
        {
            existing.Quantity += result.Quantity;
            existing.RefreshTotals();
        }
        else
        {
            var entry = new CartEntry(result);
            _cart.Add(entry);
        }

        UpdateCartBadge();
    }

    void UpdateCartBadge()
    {
        var count = _cart.Sum(c => c.Quantity);
        var total = _cart.Sum(c => c.LineTotal);
        var amountText = total > 0 ? $" · {total.ToString("$0.00", CultureInfo.CurrentCulture)}" : string.Empty;
        FabCart.Text = $"🧾 Carrito ({count}){amountText}";
    }

    async void OpenCart_Clicked(object sender, EventArgs e)
    {
        var popup = new CartReviewPopup(_cart, _orderHeaderState.Clone(), _ordersApi);
        var action = await this.ShowPopupAsync(popup) as CartPopupResult;

        UpdateCartBadge();

        if (action is null)
            return;

        switch (action.Action)
        {
            case CartPopupAction.Checkout:
                if (action.Header is null)
                {
                    await DisplayAlert("Orden", "Completa los detalles del pedido antes de enviar.", "OK");
                    return;
                }

                _orderHeaderState = action.Header.Clone();
                await CheckoutAsync(action.Header);
                break;
            case CartPopupAction.EditLine when action.LineToEdit != null:
                await EditCartEntryAsync(action.LineToEdit);
                break;
        }
    }

    async Task EditCartEntryAsync(CartEntry entry)
    {
        if (entry == null)
            return;

        await ConfigureItemAsync(entry.SelectedItem, entry);
    }

    async Task CheckoutAsync(OrderHeaderState header)
    {
        if (!Perms.OrdersCreate)
        {
            await DisplayAlert("Acceso restringido", "No tienes permiso para crear órdenes.", "OK");
            return;
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            await DisplayAlert("Sin conexión", "Necesitas Internet para crear la orden.", "OK");
            return;
        }

        static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        var payload = new CreateOrderDTO
        {
            serviceType = string.IsNullOrWhiteSpace(header.ServiceType) ? DefaultServiceType : header.ServiceType,
            source = string.IsNullOrWhiteSpace(header.Source) ? DefaultSource : header.Source,
            status = Normalize(header.Status),
            platformMarkupPct = header.PlatformMarkupPct.HasValue
                ? (int?)decimal.ToInt32(decimal.Round(header.PlatformMarkupPct.Value, MidpointRounding.AwayFromZero))
                : null,
            tableId = header.TableId,
            covers = header.Covers,
            note = Normalize(header.Note),
            customerName = Normalize(header.CustomerName),
            customerPhone = Normalize(header.CustomerPhone),
            externalRef = Normalize(header.ExternalRef),
            prepEtaMinutes = header.PrepEtaMinutes,
            servedByUserId = header.ServedByUserId
        };

        foreach (var entry in _cart)
        {
            var item = CreateOrderItemFromEntry(entry);
            if (item != null)
                payload.items.Add(item);
        }

        if (payload.items.Count == 0)
        {
            var confirmar = await DisplayAlert(
                "Orden sin productos",
                "La orden no tiene productos. Puedes crearla como borrador o agregar los ítems después. ¿Deseas continuar?",
                "Crear orden",
                "Cancelar");
            if (!confirmar)
                return;
        }

        try
        {
#if DEBUG
            try
            {
                var debugJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                await Clipboard.Default.SetTextAsync(debugJson);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Checkout] No se pudo copiar el body al portapapeles: {ex.Message}");
            }
#endif
            SetLoading(true);
            var created = await _ordersApi.CreateAsync(payload);
            if (created == null)
            {
                await DisplayAlert("Orden", "No se pudo crear la orden. Inténtalo nuevamente.", "OK");
                return;
            }

            var orderId = created.id;
            var orderCode = created.code;

            _cart.Clear();
            UpdateCartBadge();

            await DisplayAlert("Orden creada", $"Se generó la orden {orderCode}.", "OK");
            await Shell.Current.GoToAsync($"{nameof(OrderDetailPage)}?orderId={orderId}");
        }
        catch (HttpRequestException ex)
        {
            var msg = ErrorHandler.ObtenerMensajeHttp(new HttpResponseMessage(ex.StatusCode ?? HttpStatusCode.InternalServerError), ex.Message);
            await ErrorHandler.MostrarErrorUsuario(msg);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Orden – Crear desde carrito");
        }
        finally
        {
            SetLoading(false);
        }
    }

    static CreateOrderItemDTO? CreateOrderItemFromEntry(CartEntry entry)
    {
        var productId = entry.SelectedItem?.ProductId ?? entry.BaseItem?.ProductId;
        if (!productId.HasValue)
            return null;

        var dto = new CreateOrderItemDTO
        {
            productId = productId.Value,
            quantity = entry.Quantity,
            notes = string.IsNullOrWhiteSpace(entry.Notes) ? null : entry.Notes,
            modifiers = new List<OrderModifierSelectionInput>()
        };

        if (entry.SelectedItem?.VariantId.HasValue == true)
            dto.variantId = entry.SelectedItem.VariantId.Value;

        foreach (var modifier in entry.Modifiers)
        {
            foreach (var opt in modifier.Options)
            {
                if (opt.Quantity <= 0) continue;
                dto.modifiers.Add(new OrderModifierSelectionInput
                {
                    optionId = opt.OptionId,
                    quantity = opt.Quantity > 1 ? opt.Quantity : null
                });
            }
        }

        if (entry.HasComboChildren)
        {
            dto.children = entry.ComboChildren
                .Select(child =>
                {
                    var childInput = new ComboChildSelectionInput
                    {
                        productId = child.ProductId,
                        variantId = child.VariantId,
                        quantity = child.Quantity,
                        notes = string.IsNullOrWhiteSpace(child.Notes) ? null : child.Notes.Trim()
                    };

                    var childModifiers = new List<OrderModifierSelectionInput>();
                    foreach (var group in child.Modifiers ?? Array.Empty<CartModifierSelection>())
                    {
                        foreach (var opt in group.Options)
                        {
                            if (opt.Quantity <= 0) continue;
                            childModifiers.Add(new OrderModifierSelectionInput
                            {
                                optionId = opt.OptionId,
                                quantity = opt.Quantity > 1 ? opt.Quantity : null
                            });
                        }
                    }

                    if (childModifiers.Count > 0)
                        childInput.modifiers = childModifiers;

                    return childInput;
                })
                .ToList();
        }

        return dto;
    }

    #region Nested view models

    public class OrderHeaderState
    {
        public string ServiceType { get; set; } = DefaultServiceType;
        public string Source { get; set; } = DefaultSource;
        public string Status { get; set; } = "OPEN";
        public decimal? PlatformMarkupPct { get; set; }
        public int? TableId { get; set; }
        public string? TableName { get; set; }
        public int? Covers { get; set; }
        public string? Note { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? ExternalRef { get; set; }
        public int? PrepEtaMinutes { get; set; }
        public int? ServedByUserId { get; set; }

        public OrderHeaderState Clone()
        {
            return new OrderHeaderState
            {
                ServiceType = ServiceType,
                Source = Source,
                Status = Status,
                PlatformMarkupPct = PlatformMarkupPct,
                TableId = TableId,
                TableName = TableName,
                Covers = Covers,
                Note = Note,
                CustomerName = CustomerName,
                CustomerPhone = CustomerPhone,
                ExternalRef = ExternalRef,
                PrepEtaMinutes = PrepEtaMinutes,
                ServedByUserId = ServedByUserId
            };
        }

        public static OrderHeaderState CreateDefault()
        {
            var state = new OrderHeaderState();
            var userId = Preferences.Default.Get("user_id", 0);
            if (userId > 0)
                state.ServedByUserId = userId;
            return state;
        }
    }

    public class MenuOptionVm
    {
        public MenuOptionVm(MenusApi.MenuSummaryDto source)
        {
            Source = source;
            Id = source.id;
            Name = string.IsNullOrWhiteSpace(source.name) ? $"Menú #{source.id}" : source.name;
            IsActive = source.isActive;
            PublishedAt = source.publishedAt;
        }

        public MenusApi.MenuSummaryDto Source { get; }
        public int Id { get; }
        public string Name { get; }
        public bool IsActive { get; }
        public DateTime? PublishedAt { get; }
        public string DisplayName => IsActive ? Name : $"{Name} (inactivo)";

        public override string ToString() => DisplayName;
    }

    public class MenuSectionVm : INotifyPropertyChanged
    {
        readonly Color _selectedBackground = Color.FromArgb("#894164");
        readonly Color _selectedText = Colors.White;
        readonly Color _unselectedText = Color.FromArgb("#894164");

        Color _chipBackground = Colors.Transparent;
        Color _chipTextColor;
        bool _isSelected;

        public MenuSectionVm(int id, string name, int position)
        {
            Id = id;
            Name = name;
            Position = position;
            Items = new ObservableCollection<MenuItemVm>();
            _chipTextColor = _unselectedText;
        }

        public int Id { get; }
        public string Name { get; }
        public int Position { get; }
        public ObservableCollection<MenuItemVm> Items { get; }

        public bool IsSelected
        {
            get => _isSelected;
            private set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public Color ChipBackground
        {
            get => _chipBackground;
            private set
            {
                if (_chipBackground == value) return;
                _chipBackground = value;
                OnPropertyChanged();
            }
        }

        public Color ChipTextColor
        {
            get => _chipTextColor;
            private set
            {
                if (_chipTextColor == value) return;
                _chipTextColor = value;
                OnPropertyChanged();
            }
        }

        public int ItemsCount => Items.Count;

        public void RefreshChipColors()
        {
            ChipBackground = IsSelected ? _selectedBackground : Color.FromArgb("#f0ecf0");
            ChipTextColor = IsSelected ? _selectedText : _unselectedText;
        }

        public void SetSelected(bool value)
        {
            IsSelected = value;
            RefreshChipColors();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class MenuItemVm : INotifyPropertyChanged
    {
        public int Id { get; init; }
        public int SectionId { get; init; }
        public int DisplaySortOrder { get; init; }
        public string Kind { get; init; } = string.Empty;
        public int? ProductId { get; init; }
        public int? VariantId { get; init; }
        public int? OptionId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Subtitle { get; init; }
        public string? Description { get; init; }
        public string PriceLabel { get; init; } = string.Empty;
        public string ReferenceLabel { get; init; } = string.Empty;
        public bool HasReferenceLabel => !string.IsNullOrWhiteSpace(ReferenceLabel);
        public decimal UnitPrice { get; init; }
        ImageSource? _imageSource;
        public ImageSource? ImageSource
        {
            get => _imageSource;
            private set
            {
                if (_imageSource == value) return;
                _imageSource = value;
                OnPropertyChanged();
            }
        }
        public MenusApi.MenuPublicItemDto Raw { get; init; } = new();

        public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

        public bool IsVariant => string.Equals(Kind, "VARIANT", StringComparison.OrdinalIgnoreCase);
        public bool IsProduct => string.Equals(Kind, "PRODUCT", StringComparison.OrdinalIgnoreCase);
        public bool IsCombo => string.Equals(Kind, "COMBO", StringComparison.OrdinalIgnoreCase);
        public IReadOnlyList<ComboComponent> ComboComponents { get; init; } = Array.Empty<ComboComponent>();

        public bool Matches(string query)
        {
            return (Title ?? string.Empty).ToLowerInvariant().Contains(query)
                   || (Subtitle ?? string.Empty).ToLowerInvariant().Contains(query)
                   || ReferenceLabel.ToLowerInvariant().Contains(query);
        }

        public static MenuItemVm? From(MenusApi.MenuPublicSectionDto section, MenusApi.MenuPublicItemDto dto)
        {
            var reference = dto.@ref;
            string resolvedTitle = dto.displayName ?? string.Empty;
            string? subtitle = null;
            string? description = null;
            decimal? price = null;
            int? productId = null;
            int? variantId = null;
            int? optionId = null;
            ImageSource? image = null;

            var components = reference?.components?
                .Select(ComboComponent.From)
                .Where(c => c != null)
                .Cast<ComboComponent>()
                .ToList() ?? new List<ComboComponent>();

            switch (dto.refType.ToUpperInvariant())
            {
                case "PRODUCT":
                case "COMBO":
                    var product = reference?.product;
                    if (product is null) return null;
                    if (string.IsNullOrWhiteSpace(resolvedTitle))
                        resolvedTitle = product.name;
                    subtitle = product.type;
                    description = product.description;
                    price = (dto.displayPriceCents ?? product.priceCents)?.ToCurrency();
                    productId = product.id;
                    image = BuildImage(product.imageUrl);
                    break;
                case "VARIANT":
                    var variant = reference?.variant;
                    if (variant is null) return null;
                    var parent = variant.product;
                    if (string.IsNullOrWhiteSpace(resolvedTitle))
                        resolvedTitle = variant.name ?? parent?.name ?? $"Variante #{variant.id}";
                    subtitle = parent?.name;
                    description = parent?.description;
                    price = (dto.displayPriceCents ?? variant.priceCents ?? parent?.priceCents)?.ToCurrency();
                    variantId = variant.id;
                    productId = parent?.id;
                    image = BuildImage(variant.imageUrl) ?? BuildImage(parent?.imageUrl);
                    break;
                case "OPTION":
                    var option = reference?.option;
                    if (option is null) return null;
                    if (string.IsNullOrWhiteSpace(resolvedTitle))
                        resolvedTitle = option.name ?? $"Opción #{option.id}";
                    subtitle = "Opción de modificador";
                    price = (dto.displayPriceCents ?? option.priceExtraCents)?.ToCurrency();
                    optionId = option.id;
                    break;
                default:
                    return null;
            }

            if (string.IsNullOrWhiteSpace(resolvedTitle))
                resolvedTitle = $"Ítem #{dto.id}";

            var priceLabel = price.HasValue
                ? price.Value.ToString("$0.00", CultureInfo.CurrentCulture)
                : "Precio base";

            var referenceLabel = dto.refType switch
            {
                "VARIANT" => $"Variante #{dto.refId}",
                "OPTION" => $"Opción #{dto.refId}",
                "COMBO" => $"Combo #{dto.refId}",
                _ => $"Producto #{dto.refId}"
            };

            return new MenuItemVm
            {
                Id = dto.id,
                SectionId = section.id,
                DisplaySortOrder = dto.position,
                Kind = dto.refType,
                ProductId = productId,
                VariantId = variantId,
                OptionId = optionId,
                Title = resolvedTitle,
                Subtitle = subtitle,
                Description = description,
                PriceLabel = priceLabel,
                ReferenceLabel = referenceLabel,
                UnitPrice = price ?? 0m,
                ImageSource = image,
                Raw = dto,
                ComboComponents = components
            };
        }

        static ImageSource? BuildImage(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return ImageSource.FromFile("no_disponible.png");
            if (Uri.TryCreate(raw, UriKind.Absolute, out var absolute))
                return ImageSource.FromUri(absolute);

            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            var path = raw.StartsWith('/') ? raw : "/" + raw;
            if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                path = "/api" + path;
            return ImageSource.FromUri(new Uri(baseUrl + path));
        }

        public static MenuItemVm? From(MenusApi.MenuItemDto dto, string? sectionName = null)
        {
            var section = new MenusApi.MenuPublicSectionDto
            {
                id = dto.sectionId,
                name = sectionName ?? $"Sección #{dto.sectionId}",
                items = new List<MenusApi.MenuPublicItemDto>()
            };

            var item = new MenusApi.MenuPublicItemDto
            {
                id = dto.id,
                sectionId = dto.sectionId,
                refType = dto.refType,
                refId = dto.refId,
                displayName = dto.displayName,
                displayPriceCents = dto.displayPriceCents,
                position = dto.position,
                isFeatured = dto.isFeatured,
                isActive = dto.isActive,
                @ref = new MenusApi.MenuPublicReferenceDto
                {
                    kind = dto.refType,
                    product = dto.@ref?.product,
                    variant = dto.@ref?.variant,
                    option = dto.@ref?.option,
                    components = dto.@ref?.components ?? new List<MenusApi.MenuPublicComboComponent>()
                }
            };

            return From(section, item);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void SetImage(ImageSource source)
        {
            ImageSource = source;
        }

        public class ComboComponent
        {
            ComboComponent(
                int productId,
                int? variantId,
                int quantity,
                bool isRequired,
                string productName,
                string? variantName,
                string? notes,
                MenusApi.MenuPublicProductReference? productReference,
                MenusApi.MenuPublicVariantReference? variantReference)
            {
                ProductId = productId;
                VariantId = variantId;
                Quantity = quantity;
                IsRequired = isRequired;
                ProductName = productName;
                VariantName = variantName;
                Notes = notes;
                ProductReference = productReference;
                VariantReference = variantReference;
            }

            public int ProductId { get; }
            public int? VariantId { get; }
            public int Quantity { get; }
            public bool IsRequired { get; }
            public string ProductName { get; }
            public string? VariantName { get; }
            public string? Notes { get; }
            public MenusApi.MenuPublicProductReference? ProductReference { get; }
            public MenusApi.MenuPublicVariantReference? VariantReference { get; }

            public static ComboComponent? From(MenusApi.MenuPublicComboComponent source)
            {
                if (source == null)
                    return null;

                var productId = source.product?.id ?? source.variant?.product?.id ?? 0;
                if (productId <= 0)
                    return null;

                var quantity = source.quantity <= 0 ? 1 : source.quantity;
                var productReference = source.product ?? source.variant?.product;
                var productName = productReference?.name ?? $"Producto #{productId}";
                var variantName = source.variant?.name;
                var variantId = source.variant?.id;

                return new ComboComponent(
                    productId,
                    variantId,
                    quantity,
                    source.isRequired,
                    productName,
                    variantName,
                    source.notes,
                    productReference,
                    source.variant);
            }
        }
    }

    public class CartEntry : INotifyPropertyChanged
    {
        readonly List<CartModifierSelection> _modifiers = new();
        readonly List<ComboChildSelection> _comboChildren = new();

        public CartEntry(ConfigureMenuItemResult result)
        {
            LineId = Guid.NewGuid();
            BaseItem = result.BaseItem;
            SelectedItem = result.SelectedItem;
            Quantity = result.Quantity;
            Notes = result.Notes;
            foreach (var mod in result.Modifiers)
                _modifiers.Add(mod);
            if (result.Children != null)
            {
                foreach (var child in result.Children)
                    _comboChildren.Add(child);
            }
            RefreshTotals();
        }

        public Guid LineId { get; }
        public MenuItemVm BaseItem { get; }

        MenuItemVm _selectedItem;
        public MenuItemVm SelectedItem
        {
            get => _selectedItem;
            private set
            {
                if (_selectedItem == value) return;
                _selectedItem = value;
                OnPropertyChanged();
                RefreshTotals();
            }
        }

        int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set
            {
                var normalized = Math.Max(1, value);
                if (_quantity == normalized) return;
                _quantity = normalized;
                OnPropertyChanged();
                RefreshTotals();
            }
        }

        string? _notes;
        public string? Notes
        {
            get => _notes;
            set
            {
                if (_notes == value) return;
                _notes = value;
                OnPropertyChanged();
                RefreshTotals();
            }
        }

        public decimal UnitPrice { get; private set; }
        public decimal ExtrasTotal { get; private set; }
        public decimal LineTotal { get; private set; }

        public IReadOnlyList<CartModifierSelection> Modifiers => _modifiers;
        public IReadOnlyList<ComboChildSelection> ComboChildren => _comboChildren;
        public bool HasComboChildren => _comboChildren.Count > 0;

        public string Title => SelectedItem.Title;
        public string Subtitle
        {
            get
            {
                var parts = new List<string>();
                if (SelectedItem.IsVariant && SelectedItem.Subtitle != null)
                    parts.Add(SelectedItem.Subtitle);
            if (_modifiers.Any())
                parts.AddRange(_modifiers.Select(m => m.Summary));
            if (BaseItem.IsCombo && _comboChildren.Any())
            {
                var childSummaries = _comboChildren
                    .Select(c => c.HasDetail ? $"{c.DisplayLabel} ({c.Detail})" : c.DisplayLabel);
                parts.Add("Incluye: " + string.Join(", ", childSummaries));
            }
            if (!string.IsNullOrWhiteSpace(Notes))
                parts.Add($"Notas: {Notes}");
            return string.Join(" · ", parts);
        }
    }

        public bool Matches(ConfigureMenuItemResult result)
        {
            if (SelectedItem.Id != result.SelectedItem.Id)
                return false;

            if (!ModifierSummaryEquals(result.Modifiers))
                return false;

            if (!ComboChildrenEquals(result.Children))
                return false;

            return string.Equals(Notes ?? string.Empty, result.Notes ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        bool ModifierSummaryEquals(IEnumerable<CartModifierSelection> other)
        {
            var a = _modifiers.OrderBy(m => m.GroupId).ToList();
            var b = other.OrderBy(m => m.GroupId).ToList();
            if (a.Count != b.Count)
                return false;

            for (int i = 0; i < a.Count; i++)
            {
                if (!a[i].Equals(b[i]))
                    return false;
            }
            return true;
        }

        bool ComboChildrenEquals(IReadOnlyList<ComboChildSelection>? other)
        {
            var otherList = other ?? Array.Empty<ComboChildSelection>();
            if (_comboChildren.Count != otherList.Count)
                return false;

            var left = _comboChildren
                .OrderBy(c => c.ProductId)
                .ThenBy(c => c.VariantId ?? -1)
                .ThenBy(c => c.Quantity)
                .ToList();

            var right = otherList
                .OrderBy(c => c.ProductId)
                .ThenBy(c => c.VariantId ?? -1)
                .ThenBy(c => c.Quantity)
                .ToList();

            for (int i = 0; i < left.Count; i++)
            {
                var a = left[i];
                var b = right[i];
                if (a.ProductId != b.ProductId || a.VariantId != b.VariantId || a.Quantity != b.Quantity)
                    return false;
                if (!string.Equals(a.Notes ?? string.Empty, b.Notes ?? string.Empty, StringComparison.Ordinal))
                    return false;
                if (!ModifierSelectionsEqual(a.Modifiers, b.Modifiers))
                    return false;
            }

            return true;
        }

        static bool ModifierSelectionsEqual(IReadOnlyList<CartModifierSelection>? left, IReadOnlyList<CartModifierSelection>? right)
        {
            if (ReferenceEquals(left, right))
                return true;

            left ??= Array.Empty<CartModifierSelection>();
            right ??= Array.Empty<CartModifierSelection>();

            if (left.Count != right.Count)
                return false;

            var orderedLeft = left.OrderBy(m => m.GroupId).ToList();
            var orderedRight = right.OrderBy(m => m.GroupId).ToList();
            for (int i = 0; i < orderedLeft.Count; i++)
            {
                if (!orderedLeft[i].Equals(orderedRight[i]))
                    return false;
            }

            return true;
        }

        public void RefreshTotals()
        {
            UnitPrice = SelectedItem.UnitPrice;
            var comboExtras = _comboChildren.Sum(child => child.Modifiers?.Sum(m => m.TotalExtra) ?? 0m);
            ExtrasTotal = _modifiers.Sum(m => m.TotalExtra) + comboExtras;
            LineTotal = (UnitPrice + ExtrasTotal) * Quantity;
            OnPropertyChanged(nameof(UnitPrice));
            OnPropertyChanged(nameof(ExtrasTotal));
            OnPropertyChanged(nameof(LineTotal));
            OnPropertyChanged(nameof(Subtitle));
        }

        public bool IsEquivalent(ConfigureMenuItemResult result) => Matches(result);

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class CartModifierSelection : IEquatable<CartModifierSelection>
    {
        public CartModifierSelection(int groupId, string groupName, IEnumerable<ModifierOptionSelection> options)
        {
            GroupId = groupId;
            GroupName = groupName;
            Options = options.ToList();
        }

        public int GroupId { get; }
        public string GroupName { get; }
        public List<ModifierOptionSelection> Options { get; }

        public decimal TotalExtra => Options.Sum(o => o.TotalExtra);

        public string Summary
        {
            get
            {
                var detail = string.Join(", ", Options.Select(o => o.DisplayName));
                return $"{GroupName}: {detail}";
            }
        }

        public bool Equals(CartModifierSelection? other)
        {
            if (other is null) return false;
            if (GroupId != other.GroupId) return false;
            if (Options.Count != other.Options.Count) return false;
            for (int i = 0; i < Options.Count; i++)
            {
                if (!Options[i].Equals(other.Options[i]))
                    return false;
            }
            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as CartModifierSelection);
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(GroupId);
            foreach (var opt in Options)
                hash.Add(opt);
            return hash.ToHashCode();
        }
    }

    public record ComboChildSelection(int ProductId, int? VariantId, int Quantity, bool IsRequired, string ProductName, string? VariantName, string? Notes, IReadOnlyList<CartModifierSelection> Modifiers)
    {
        public string DisplayLabel
        {
            get
            {
                var name = string.IsNullOrWhiteSpace(VariantName)
                    ? ProductName
                    : $"{ProductName} · {VariantName}";
                return Quantity > 1 ? $"{Quantity} × {name}" : name;
            }
        }

        public string? Detail
        {
            get
            {
                var parts = new List<string>();
                if (Modifiers != null && Modifiers.Count > 0)
                {
                    parts.AddRange(
                        Modifiers.Select(m => $"{m.GroupName}: {string.Join(", ", m.Options.Select(o => o.DisplayName))}"));
                }

                if (!string.IsNullOrWhiteSpace(Notes))
                    parts.Add($"Notas: {Notes}");

                return parts.Count == 0 ? null : string.Join(" • ", parts);
            }
        }

        public bool HasDetail => !string.IsNullOrWhiteSpace(Detail);
    }

    public class ModifierOptionSelection : IEquatable<ModifierOptionSelection>
    {
        public ModifierOptionSelection(int id, string name, decimal priceExtra, int quantity)
        {
            OptionId = id;
            Name = name;
            PriceExtra = priceExtra;
            Quantity = Math.Max(0, quantity);
        }

        public int OptionId { get; }
        public string Name { get; }
        public decimal PriceExtra { get; }
        public int Quantity { get; }
        public decimal TotalExtra => PriceExtra * Quantity;
        public string DisplayName
        {
            get
            {
                var baseLabel = PriceExtra > 0
                    ? $"{Name} (+{PriceExtra.ToString("$0.00", CultureInfo.CurrentCulture)})"
                    : Name;
                return Quantity > 1 ? $"{baseLabel} x{Quantity}" : baseLabel;
            }
        }

        public bool Equals(ModifierOptionSelection? other)
        {
            if (other is null) return false;
            return OptionId == other.OptionId && Quantity == other.Quantity;
        }

        public override bool Equals(object? obj) => Equals(obj as ModifierOptionSelection);
        public override int GetHashCode() => HashCode.Combine(OptionId, Quantity);
    }

    #endregion
}
