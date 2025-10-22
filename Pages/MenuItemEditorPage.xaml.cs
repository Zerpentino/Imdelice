using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Networking;

namespace Imdeliceapp.Pages;

[QueryProperty(nameof(Mode), "mode")]
[QueryProperty(nameof(MenuId), "menuId")]
[QueryProperty(nameof(MenuName), "menuName")]
[QueryProperty(nameof(SectionId), "sectionId")]
[QueryProperty(nameof(SectionName), "sectionName")]
[QueryProperty(nameof(ItemId), "itemId")]
[QueryProperty(nameof(InitialRefType), "refType")]
[QueryProperty(nameof(InitialRefId), "refId")]
[QueryProperty(nameof(InitialDisplayName), "displayName")]
[QueryProperty(nameof(InitialDisplayPrice), "displayPriceCents")]
[QueryProperty(nameof(InitialPosition), "position")]
[QueryProperty(nameof(InitialFeatured), "isFeatured")]
[QueryProperty(nameof(InitialActive), "isActive")]
public partial class MenuItemEditorPage : ContentPage
{
    readonly MenusApi _menus = new();
    readonly ModifiersApi _modifiers = new();

    public string Mode { get; set; } = "create";
    public int MenuId { get; set; }
    public string? MenuName { get; set; }
    public int SectionId { get; set; }
    public string? SectionName { get; set; }
    public int ItemId { get; set; }

    public string? InitialRefType { get; set; }
    public string? InitialRefId { get; set; }
    public string? InitialDisplayName { get; set; }
    public string? InitialDisplayPrice { get; set; }
    public string? InitialPosition { get; set; }
    public string? InitialFeatured { get; set; }
    public string? InitialActive { get; set; }

    bool _isSaving;
    bool IsEdit => string.Equals(Mode, "edit", StringComparison.OrdinalIgnoreCase);

    string? _selectedRefType;
    int? _selectedRefId;
    string _selectedRefLabel = "Ninguna";
    bool _suppressTypeChanged;

    public MenuItemEditorPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            await DisplayAlert("Sin conexión", "Necesitas Internet para gestionar ítems.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        await PopulateAsync();
    }

    async Task PopulateAsync()
    {
        HeaderLabel.Text = IsEdit ? "Editar ítem" : "Nuevo ítem";
        InfoLabel.Text = string.IsNullOrWhiteSpace(MenuName)
            ? $"Sección #{SectionId}"
            : $"{MenuName} · {SectionName ?? "Sección"}";

        BusyIndicator.IsRunning = BusyIndicator.IsVisible = false;

        if (IsEdit)
        {
            RefTypePicker.IsEnabled = false;
            SelectRefButton.IsEnabled = false;

            _selectedRefType = InitialRefType;
            _selectedRefId = int.TryParse(InitialRefId, out var rid) ? rid : null;
            _selectedRefLabel = !string.IsNullOrWhiteSpace(InitialDisplayName)
                ? InitialDisplayName!
                : _selectedRefId.HasValue && !string.IsNullOrWhiteSpace(_selectedRefType)
                    ? $"{_selectedRefType} #{_selectedRefId}"
                    : "Ninguna";

            EnsureRefTypePicker(_selectedRefType);
            UpdateReferenceUI();

            DisplayNameEntry.Text = InitialDisplayName;
            PriceEntry.Text = InitialDisplayPrice;
            PositionEntry.Text = InitialPosition;
            FeaturedSwitch.IsToggled = bool.TryParse(InitialFeatured, out var f) && f;
            ActiveSwitch.IsToggled = !bool.TryParse(InitialActive, out var a) || a;
        }
        else
        {
            RefTypePicker.IsEnabled = true;
            SelectRefButton.IsEnabled = true;
            RefTypePicker.SelectedIndex = 0;
            _selectedRefType = RefTypePicker.SelectedItem as string;
            ActiveSwitch.IsToggled = true;
            FeaturedSwitch.IsToggled = false;
            _selectedRefLabel = "Ninguna";
            UpdateReferenceUI();
        }
    }

    void RefTypePicker_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (IsEdit || _suppressTypeChanged) return;
        _selectedRefType = RefTypePicker.SelectedItem as string;
        _selectedRefId = null;
        _selectedRefLabel = "Ninguna";
        UpdateReferenceUI();
    }

    async void SelectReference_Clicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedRefType))
        {
            await DisplayAlert("Referencia", "Selecciona un tipo primero.", "OK");
            return;
        }

        try
        {
            switch (_selectedRefType)
            {
                case "PRODUCT":
                    await PickProductAsync(includeCombos: false);
                    break;
                case "COMBO":
                    await PickProductAsync(includeCombos: true);
                    break;
                case "VARIANT":
                    await PickVariantAsync();
                    break;
                case "OPTION":
                    await PickModifierOptionAsync();
                    break;
                default:
                    await DisplayAlert("Referencia", "Tipo no soportado.", "OK");
                    break;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Referencia", ex.Message, "OK");
        }
    }

    async Task PickProductAsync(bool includeCombos)
    {
        bool Filter(ProductPickerPage.ProductDTO p)
        {
            if (!p.isActive) return false;
            var type = p.type?.ToUpperInvariant() ?? string.Empty;
            if (includeCombos)
                return type == "COMBO";
            return type == "SIMPLE";
        }

        var product = await ProductPickerPage.PickAsync(Navigation, Filter);
        if (product is null) return;

        _selectedRefId = product.id;
        _selectedRefLabel = product.name;
        _selectedRefType = includeCombos ? "COMBO" : "PRODUCT";
        EnsureRefTypePicker(_selectedRefType);
        UpdateReferenceUI();

        if (string.IsNullOrWhiteSpace(DisplayNameEntry.Text))
            DisplayNameEntry.Text = product.name;
    }

    async Task PickVariantAsync()
    {
        static bool Filter(ProductPickerPage.ProductDTO p)
            => p.isActive && string.Equals(p.type, "VARIANTED", StringComparison.OrdinalIgnoreCase);

        var product = await ProductPickerPage.PickAsync(Navigation, Filter);
        if (product is null) return;

        var detail = await _menus.GetProductAsync(product.id);
        if (detail is null)
        {
            await DisplayAlert("Variantes", "No se pudo obtener la información del producto.", "OK");
            return;
        }

        var variants = detail.variants?
            .Where(v => (v.isActive ?? true) && (v.isAvailable ?? true))
            .ToList();

        if (variants == null || variants.Count == 0)
        {
            await DisplayAlert("Variantes", "Ese producto no tiene variantes activas disponibles.", "OK");
            return;
        }

        var options = variants
            .Select(v => (Title: string.IsNullOrWhiteSpace(v.name) ? $"Variante #{v.id}" : v.name!, Variant: v))
            .ToArray();

        var choice = await DisplayActionSheet("Selecciona variante", "Cancelar", null, options.Select(o => o.Title).ToArray());
        if (string.IsNullOrWhiteSpace(choice) || choice == "Cancelar") return;

        var picked = options.FirstOrDefault(o => o.Title == choice).Variant;
        if (picked == null) return;

        _selectedRefId = picked.id;
        _selectedRefType = "VARIANT";
        EnsureRefTypePicker(_selectedRefType);
        _selectedRefLabel = string.IsNullOrWhiteSpace(picked.name)
            ? $"{product.name} · Variante #{picked.id}"
            : $"{product.name} · {picked.name}";
        UpdateReferenceUI();

        if (string.IsNullOrWhiteSpace(DisplayNameEntry.Text))
            DisplayNameEntry.Text = picked.name ?? product.name;
    }

    async Task PickModifierOptionAsync()
    {
        var groups = await _modifiers.GetGroupsAsync(isActive: true);
        var active = groups
            .Where(g => g.isActive && (g.options?.Any(o => o.isActive) ?? false))
            .OrderBy(g => g.name)
            .ToList();

        if (active.Count == 0)
        {
            await DisplayAlert("Opciones", "No hay opciones activas disponibles.", "OK");
            return;
        }

        var groupChoice = await DisplayActionSheet("Grupo de modificadores", "Cancelar", null, active.Select(g => g.name ?? $"Grupo #{g.id}").ToArray());
        if (string.IsNullOrWhiteSpace(groupChoice) || groupChoice == "Cancelar") return;

        var group = active.First(g => (g.name ?? $"Grupo #{g.id}") == groupChoice);
        var options = group.options?
            .Where(o => o.isActive)
            .OrderBy(o => o.name)
            .Select(o => (o.id, Title: string.IsNullOrWhiteSpace(o.name) ? $"Opción #{o.id}" : o.name!))
            .ToList();

        if (options == null || options.Count == 0)
        {
            await DisplayAlert("Opciones", "Ese grupo no tiene opciones activas.", "OK");
            return;
        }

        var optChoice = await DisplayActionSheet("Opción", "Cancelar", null, options.Select(o => o.Title).ToArray());
        if (string.IsNullOrWhiteSpace(optChoice) || optChoice == "Cancelar") return;

        var opt = options.First(o => o.Title == optChoice);
        _selectedRefId = opt.id;
        _selectedRefType = "OPTION";
        EnsureRefTypePicker(_selectedRefType);
        _selectedRefLabel = $"{group.name ?? "Grupo"} · {opt.Title}";
        UpdateReferenceUI();

        if (string.IsNullOrWhiteSpace(DisplayNameEntry.Text))
            DisplayNameEntry.Text = opt.Title;
    }

    async void Save_Clicked(object? sender, EventArgs e)
    {
        if (_isSaving) return;

        if (!IsEdit && (_selectedRefId is null || string.IsNullOrWhiteSpace(_selectedRefType)))
        {
            await DisplayAlert("Ítem", "Selecciona una referencia.", "OK");
            return;
        }

        var (priceOk, priceCents) = await ParsePriceAsync();
        if (!priceOk) return;
        var (posOk, position) = await ParsePositionAsync();
        if (!posOk) return;

        var displayName = string.IsNullOrWhiteSpace(DisplayNameEntry.Text) ? null : DisplayNameEntry.Text.Trim();
        var dtoUpdate = new MenusApi.MenuItemUpdateDto
        {
            displayName = displayName,
            displayPriceCents = priceCents,
            position = position,
            isFeatured = FeaturedSwitch.IsToggled,
            isActive = ActiveSwitch.IsToggled
        };

        var dtoCreate = new MenusApi.MenuItemCreateDto
        {
            sectionId = SectionId,
            refType = _selectedRefType ?? "PRODUCT",
            refId = _selectedRefId ?? 0,
            displayName = displayName,
            displayPriceCents = priceCents,
            position = position,
            isFeatured = FeaturedSwitch.IsToggled,
            isActive = ActiveSwitch.IsToggled
        };

        try
        {
            BusyIndicator.IsRunning = BusyIndicator.IsVisible = _isSaving = true;

            if (IsEdit)
            {
                await _menus.UpdateMenuItemAsync(ItemId, dtoUpdate);
            }
            else
            {
                await _menus.CreateMenuItemAsync(dtoCreate);
            }

            await DisplayAlert("Listo", IsEdit ? "Ítem actualizado." : "Ítem creado.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            BusyIndicator.IsRunning = BusyIndicator.IsVisible = _isSaving = false;
        }
    }

    async Task<(bool ok, int? value)> ParsePriceAsync()
    {
        int? cents = null;
        var raw = PriceEntry.Text?.Trim();
        if (string.IsNullOrEmpty(raw)) return (true, null);

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var direct) && direct >= 0)
        {
            cents = direct;
            return (true, cents);
        }

        if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out var dec))
        {
            if (dec < 0)
            {
                await DisplayAlert("Precio", "El precio no puede ser negativo.", "OK");
                return (false, null);
            }
            cents = (int)Math.Round(dec * 100, MidpointRounding.AwayFromZero);
            return (true, cents);
        }

        await DisplayAlert("Precio", "Ingresa un valor válido (entero en centavos o decimal).", "OK");
        return (false, null);
    }

    async Task<(bool ok, int? value)> ParsePositionAsync()
    {
        int? position = null;
        var raw = PositionEntry.Text?.Trim();
        if (string.IsNullOrEmpty(raw)) return (true, null);
        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pos) && pos >= 0)
        {
            position = pos;
            return (true, position);
        }

        await DisplayAlert("Posición", "Ingresa un entero mayor o igual a 0.", "OK");
        return (false, null);
    }

    void ClearReference_Clicked(object sender, EventArgs e)
    {
        if (IsEdit) return;
        _selectedRefId = null;
        _selectedRefLabel = "Ninguna";
        UpdateReferenceUI();
    }

    void UpdateReferenceUI()
    {
        SelectedRefLabel.Text = _selectedRefLabel;
        if (ClearRefButton != null)
            ClearRefButton.IsVisible = !IsEdit && _selectedRefId.HasValue;
    }

    void EnsureRefTypePicker(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return;
        var idx = RefTypePicker.Items.IndexOf(type);
        if (idx >= 0)
        {
            _suppressTypeChanged = true;
            RefTypePicker.SelectedIndex = idx;
            _suppressTypeChanged = false;
        }
    }
}
