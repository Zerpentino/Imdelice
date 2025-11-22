using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Imdeliceapp.Helpers;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using Imdeliceapp.Models;
using Perms = Imdeliceapp.Services.Perms;

namespace Imdeliceapp.Pages
{
    [QueryProperty(nameof(LocationId), "locationId")]
    public partial class InventoryProductsPage : ContentPage
    {
    readonly InventoryApi _inventoryApi = new();
    readonly Dictionary<string, ImageSource> _imageCache = new();

    public ObservableCollection<InventoryProductVm> Products { get; } = new();
    public ObservableCollection<InventoryLocationDTO> Locations { get; } = new();
    bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set
        {
            if (_isRefreshing == value) return;
            _isRefreshing = value;
            OnPropertyChanged();
        }
    }
    string _emptyMessage = "No hay insumos registrados.";
    public string EmptyMessage
    {
        get => _emptyMessage;
        set
        {
            if (_emptyMessage == value) return;
            _emptyMessage = value;
            OnPropertyChanged();
        }
    }

    List<InventoryProductVm> _allProducts = new();
    InventoryLocationDTO? _selectedLocation;
    int? _pendingLocationId;
    int _filterIndex = 0; // 0 = todos, 1 = insumos, 2 = productos con movimientos

    public InventoryProductsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public string LocationId
    {
        set
        {
            if (int.TryParse(value, out var id))
                _pendingLocationId = id;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!Perms.InventoryRead)
        {
            await DisplayAlert("Acceso restringido", "No puedes ver inventario.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        if (FilterPicker.SelectedIndex < 0) FilterPicker.SelectedIndex = 0;
        LocationPicker.IsEnabled = Perms.InventoryRead;
        FilterPicker.IsEnabled = Perms.InventoryRead;
        SearchBar.IsEnabled = Perms.InventoryRead;
        RefreshView.IsEnabled = Perms.InventoryRead;
        MovementsToolbar.IsEnabled = Perms.InventoryRead;

        await LoadLocationsAsync();
        await LoadProductsAsync();

        FabButton.IsVisible = Perms.InventoryAdjust;
    }

    async Task LoadLocationsAsync()
    {
        try
        {
            Locations.Clear();
            var list = await _inventoryApi.ListLocationsAsync();
            foreach (var loc in list)
                Locations.Add(loc);

            _selectedLocation = list.FirstOrDefault(l => l.isDefault) ?? list.FirstOrDefault();
            if (_pendingLocationId.HasValue)
            {
                var match = list.FirstOrDefault(l => l.id == _pendingLocationId.Value);
                if (match != null)
                    _selectedLocation = match;
            }

            if (_selectedLocation != null)
                LocationPicker.SelectedItem = _selectedLocation;
        }
        catch (HttpRequestException ex)
        {
            var message = ErrorHandler.ObtenerMensajeHttp(
                new HttpResponseMessage(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError),
                ex.Message);
            await ErrorHandler.MostrarErrorUsuario(message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Ubicaciones");
        }
    }

    async Task LoadProductsAsync()
    {
        try
        {
            IsRefreshing = true;
            // Nuevo endpoint ya devuelve los insumos y los productos con movimientos.
            var items = await _inventoryApi.ListItemsAsync(
                locationId: _selectedLocation?.id);

            _allProducts = items
                .GroupBy(i => i.productId)
                .Select(group => InventoryProductVm.From(group.First(), group.Sum(g => g.currentQuantity), group.First().unit))
                .OrderByDescending(p => p.IsActive)
                .ThenByDescending(p => p.HasServerImage)
                .ThenBy(p => p.Name)
                .ToList();

            ApplyFilter(SearchBar.Text);
            await LoadThumbnailsAsync(_allProducts);
        }
        catch (HttpRequestException ex)
        {
            var message = ErrorHandler.ObtenerMensajeHttp(
                new HttpResponseMessage(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError),
                ex.Message);
            await ErrorHandler.MostrarErrorUsuario(message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Productos");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    void ApplyFilter(string? query)
    {
        var q = (query ?? string.Empty).Trim().ToLowerInvariant();
        IEnumerable<InventoryProductVm> src = _allProducts;

        src = _filterIndex switch
        {
            1 => src.Where(p => p.IsInventoryCategory),
            2 => src.Where(p => !p.IsInventoryCategory),
            _ => src
        };

        if (!string.IsNullOrWhiteSpace(q))
            src = src.Where(p =>
                p.Name.ToLowerInvariant().Contains(q) ||
                p.SkuDisplay.ToLowerInvariant().Contains(q));

        Products.Clear();
        foreach (var item in src)
            Products.Add(item);

        EmptyMessage = Products.Count == 0
            ? "No encontramos insumos con ese criterio."
            : string.Empty;
    }

    void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter(e.NewTextValue);
    }

    async void RefreshView_Refreshing(object sender, EventArgs e)
    {
        await LoadProductsAsync();
        RefreshView.IsRefreshing = false;
    }

    async void EditSwipe_Invoked(object sender, EventArgs e)
    {
        if ((sender as SwipeItem)?.BindingContext is not InventoryProductVm vm)
            return;

        if (!Perms.InventoryAdjust)
        {
            await DisplayAlert("Acceso restringido", "No puedes editar inventario.", "OK");
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(InventoryInsumoEditorPage)}?productId={vm.Id}");
    }

    async void MovementsSwipe_Invoked(object sender, EventArgs e)
    {
        if ((sender as SwipeItem)?.BindingContext is not InventoryProductVm vm)
            return;

        if (vm.ItemId.HasValue)
        {
            await Shell.Current.GoToAsync($"{nameof(InventoryMovementsPage)}?itemId={vm.ItemId.Value}");
        }
        else
        {
            await Shell.Current.GoToAsync($"{nameof(InventoryMovementsPage)}?productId={vm.Id}");
        }
    }

    async void MovementsToolbar_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(InventoryMovementsPage));
    }

    async void FabButton_Clicked(object sender, EventArgs e)
    {
        if (!Perms.InventoryAdjust)
        {
            await DisplayAlert("Acceso restringido", "No puedes crear inventario.", "OK");
            return;
        }

        await Shell.Current.GoToAsync(nameof(InventoryInsumoEditorPage));
    }

    async void LocationPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        _selectedLocation = LocationPicker.SelectedItem as InventoryLocationDTO;
        await LoadProductsAsync();
    }

    async void FilterPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        _filterIndex = FilterPicker.SelectedIndex <= 0 ? 0 : FilterPicker.SelectedIndex;
        ApplyFilter(SearchBar.Text);
    }

    async Task LoadThumbnailsAsync(IEnumerable<InventoryProductVm> items)
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token)) return;

        var baseUrlObj = Application.Current?.Resources["urlbase"];
        var baseUrl = baseUrlObj?.ToString()?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl)) return;

        using var http = NewAuthClient(baseUrl, token);
        var sem = new SemaphoreSlim(4);
        var tasks = items.Select(async vm =>
        {
            if (vm.ImageLoaded) return;
            await sem.WaitAsync();
            try
            {
                vm.Image = await GetThumbAsync(vm, http);
                vm.ImageLoaded = true;
            }
            finally
            {
                sem.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    async Task<ImageSource> GetThumbAsync(InventoryProductVm vm, HttpClient http)
    {
        var cacheKey = !string.IsNullOrWhiteSpace(vm.ImageUrl) ? vm.ImageUrl! : $"id:{vm.Id}";
        if (_imageCache.TryGetValue(cacheKey, out var cached))
            return cached;

        string? path = null;
        if (!string.IsNullOrWhiteSpace(vm.ImageUrl))
        {
            path = vm.ImageUrl!;
            if (!path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                if (!path.StartsWith('/'))
                    path = "/" + path;
                if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                    path = "/api" + path;
            }
        }
        else if (vm.HasServerImage)
        {
            path = $"/api/products/{vm.Id}/image?ts={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        }

        if (string.IsNullOrWhiteSpace(path))
            return ImageSource.FromFile("no_disponible.png");

        try
        {
            using var resp = await http.GetAsync(path);
            if (!resp.IsSuccessStatusCode)
                return _imageCache[cacheKey] = ImageSource.FromFile("no_disponible.png");

            var bytes = await resp.Content.ReadAsByteArrayAsync();
            var src = ImageSource.FromStream(() => new MemoryStream(bytes));
            _imageCache[cacheKey] = src;
            return src;
        }
        catch
        {
            return _imageCache[cacheKey] = ImageSource.FromFile("no_disponible.png");
        }
    }

    static async Task<string?> GetTokenAsync()
    {
        try
        {
            var secure = await SecureStorage.GetAsync("token");
            if (!string.IsNullOrWhiteSpace(secure)) return secure;
        }
        catch
        {
            // ignored
        }

        var pref = Preferences.Default.Get("token", string.Empty);
        return string.IsNullOrWhiteSpace(pref) ? null : pref;
    }

    static HttpClient NewAuthClient(string baseUrl, string token)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(20)
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

class QuantityInfo
{
    public decimal Quantity { get; init; }
    public string Unit { get; init; } = string.Empty;
}

public class InventoryProductVm : INotifyPropertyChanged
{
    public int? ItemId { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string SkuDisplay { get; set; } = "-";
    public string? CategoryName { get; set; }
    public string? CategorySlug { get; set; }
    public bool IsActive { get; set; }
    public string Status => IsActive ? "Activo" : "Inactivo";
    public Color StatusColor => IsActive ? Color.FromArgb("#2E7D32") : Color.FromArgb("#B71C1C");
    public string? ImageUrl { get; set; }
    public bool HasServerImage { get; set; }
    public bool ImageLoaded { get; set; }
    decimal? _quantity;
    string? _unit;

    ImageSource _image = ImageSource.FromFile("no_disponible.png");
    public ImageSource Image
    {
        get => _image;
        set
        {
            if (_image == value) return;
            _image = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Image)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsInventoryCategory =>
        (CategorySlug ?? CategoryName ?? string.Empty)
            .Contains("inventario", StringComparison.OrdinalIgnoreCase);

    public string QuantityDisplay =>
        _quantity.HasValue
            ? $"{_quantity.Value:0.##} {_unit ?? "UNID"}"
            : "Sin existencias registradas";

    public Color QuantityColor =>
        _quantity.HasValue && _quantity.Value > 0 ? Color.FromArgb("#2E7D32") : Color.FromArgb("#B71C1C");

    public static InventoryProductVm From(ProductsApi.ProductSummaryDTO dto)
    {
        return new InventoryProductVm
        {
            Id = dto.id,
            Name = dto.name ?? $"Producto #{dto.id}",
            Type = dto.type ?? "SIMPLE",
            SkuDisplay = string.IsNullOrWhiteSpace(dto.sku) ? "-" : dto.sku!,
            IsActive = dto.isActive,
            ImageUrl = dto.imageUrl,
            HasServerImage = dto.hasImage,
            ImageLoaded = false,
            Image = ImageSource.FromFile("no_disponible.png")
        };
    }

    public void ApplyQuantity(decimal? quantity, string? unit)
    {
        var changed = _quantity != quantity || _unit != unit;
        _quantity = quantity;
        _unit = TranslateUnit(unit);
        if (changed)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(QuantityDisplay)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(QuantityColor)));
        }
    }

    static string? TranslateUnit(string? unit)
    {
        if (string.IsNullOrWhiteSpace(unit)) return null;
        return unit.ToUpperInvariant() switch
        {
            "UNIT" => "unidad(es)",
            "KILOGRAM" => "kg",
            "GRAM" => "g",
            "LITER" => "L",
            "MILLILITER" => "mL",
            _ => unit
        };
    }

    public static InventoryProductVm From(InventoryItemDTO item, decimal totalQty, string? unit)
    {
        var prod = item.product;
        var id = prod?.id ?? item.productId;
        var categoryName = prod?.categoryName;
        var categorySlug = prod?.categorySlug;

        var vm = new InventoryProductVm
        {
            Id = id,
            Name = prod?.name ?? $"Producto #{id}",
            Type = "SIMPLE",
            SkuDisplay = string.IsNullOrWhiteSpace(prod?.sku) ? "-" : prod!.sku!,
            IsActive = true,
            ImageUrl = prod?.imageUrl,
            HasServerImage = prod?.hasImage ?? false,
            ImageLoaded = false,
            Image = ImageSource.FromFile("no_disponible.png"),
            CategoryName = categoryName,
            CategorySlug = categorySlug,
            ItemId = item.id
        };

        vm.ApplyQuantity(totalQty, unit);
        return vm;
    }
}
}
