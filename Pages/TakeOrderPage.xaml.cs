using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using Imdeliceapp.Models;
using Imdeliceapp.Popups;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;

namespace Imdeliceapp.Pages;

public partial class TakeOrderPage : ContentPage
{
    readonly MenusApi _menusApi = new();
    readonly ModifiersApi _modifiersApi = new();

    readonly Dictionary<int, List<MenuItemVm>> _itemsByProduct = new();
    readonly Dictionary<int, List<ModifierGroupDTO>> _modifierCache = new();
    readonly List<MenuSectionVm> _allSections = new();
    readonly List<CartEntry> _cart = new();

    bool _hasLoaded;
    bool _isInitializingMenu;
    string _searchQuery = string.Empty;

    public ObservableCollection<MenuOptionVm> MenuOptions { get; } = new();
    public ObservableCollection<MenuSectionVm> Sections { get; } = new();
    public ObservableCollection<MenuItemVm> VisibleItems { get; } = new();

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

        if (_hasLoaded)
            return;

        bool menusReady = false;
        bool menuContentReady = false;

        SetLoading(true);
        try
        {
            menusReady = await EnsureMenusLoadedAsync();
            if (!menusReady)
                return;

            if (SelectedMenu != null)
            {
                menuContentReady = await LoadMenuSectionsAsync(SelectedMenu.Id, false);
            }
            else
            {
                await DisplayAlert("Menú", "Selecciona un menú para continuar.", "OK");
            }
        }
        finally
        {
            SetLoading(false);
            _hasLoaded = menusReady && menuContentReady;
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
            _allSections.Clear();

            foreach (var section in menu.sections
                     .Where(s => s.isActive)
                     .OrderBy(s => s.position)
                     .ThenBy(s => s.name, StringComparer.CurrentCultureIgnoreCase))
            {
                var vmSection = new MenuSectionVm(section.id, section.name ?? $"Sección #{section.id}", section.position);

                foreach (var item in section.items
                             .Where(i => i.isActive)
                             .OrderBy(i => i.position)
                             .ThenBy(i => i.displayName, StringComparer.CurrentCultureIgnoreCase))
                {
                    var vm = MenuItemVm.From(section, item);
                    if (vm == null)
                        continue;

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

        var baseItem = editingEntry?.BaseItem ?? item;
        var targetProductId = baseItem.ProductId ?? item.ProductId;

        var relatedVariants = new List<MenuItemVm>();
        if (targetProductId.HasValue && _itemsByProduct.TryGetValue(targetProductId.Value, out var set))
            relatedVariants = set.Where(v => v != null).ToList();

        relatedVariants.Add(baseItem);
        relatedVariants = relatedVariants
            .GroupBy(v => v.Id)
            .Select(g => g.First())
            .OrderBy(v => v.DisplaySortOrder)
            .ThenBy(v => v.Title)
            .ToList();

        var modifierGroups = new List<ModifierGroupDTO>();
        if (targetProductId.HasValue)
            modifierGroups = await GetModifierGroupsAsync(targetProductId.Value);

        var popup = new ConfigureMenuItemPopup(baseItem, relatedVariants, modifierGroups, editingEntry);
        var result = await this.ShowPopupAsync(popup) as ConfigureMenuItemResult;
        if (result is null)
            return;

        AddOrUpdateCart(result);
    }

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
        if (_cart.Count == 0)
        {
            await DisplayAlert("Carrito", "No has agregado productos aún.", "OK");
            return;
        }

        var popup = new CartReviewPopup(_cart);
        var action = await this.ShowPopupAsync(popup) as CartPopupResult;

        UpdateCartBadge();

        if (action is null)
            return;

        switch (action.Action)
        {
            case CartPopupAction.Checkout:
                await DisplayAlert("Orden", "Aquí iría el flujo para enviar la orden.", "OK");
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

    #region Nested view models

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

    public class MenuItemVm
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
        public string? ImageSource { get; init; }
        public MenusApi.MenuPublicItemDto Raw { get; init; } = new();

        public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

        public bool IsVariant => string.Equals(Kind, "VARIANT", StringComparison.OrdinalIgnoreCase);
        public bool IsProduct => string.Equals(Kind, "PRODUCT", StringComparison.OrdinalIgnoreCase);
        public bool IsCombo => string.Equals(Kind, "COMBO", StringComparison.OrdinalIgnoreCase);

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
            string? image = null;

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
                Raw = dto
            };
        }
    }

    public class CartEntry : INotifyPropertyChanged
    {
        readonly List<CartModifierSelection> _modifiers = new();

        public CartEntry(ConfigureMenuItemResult result)
        {
            LineId = Guid.NewGuid();
            BaseItem = result.BaseItem;
            SelectedItem = result.SelectedItem;
            Quantity = result.Quantity;
            Notes = result.Notes;
            foreach (var mod in result.Modifiers)
                _modifiers.Add(mod);
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

        public void RefreshTotals()
        {
            UnitPrice = SelectedItem.UnitPrice;
            ExtrasTotal = _modifiers.Sum(m => m.TotalExtra);
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

        public decimal TotalExtra => Options.Sum(o => o.PriceExtra);

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
        public override int GetHashCode() => HashCode.Combine(GroupId, Options.Count);
    }

    public class ModifierOptionSelection : IEquatable<ModifierOptionSelection>
    {
        public ModifierOptionSelection(int id, string name, decimal priceExtra)
        {
            OptionId = id;
            Name = name;
            PriceExtra = priceExtra;
        }

        public int OptionId { get; }
        public string Name { get; }
        public decimal PriceExtra { get; }
        public string DisplayName => PriceExtra > 0 ? $"{Name} (+{PriceExtra.ToString("$0.00", CultureInfo.CurrentCulture)})" : Name;

        public bool Equals(ModifierOptionSelection? other)
        {
            if (other is null) return false;
            return OptionId == other.OptionId;
        }

        public override bool Equals(object? obj) => Equals(obj as ModifierOptionSelection);
        public override int GetHashCode() => OptionId.GetHashCode();
    }

    #endregion
}

static class DecimalExtensions
{
    public static decimal ToCurrency(this int cents) => cents / 100m;

    public static decimal? ToCurrency(this int? cents)
        => cents.HasValue ? cents.Value / 100m : (decimal?)null;
}
