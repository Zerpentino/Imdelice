using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Imdeliceapp.Helpers;
using Imdeliceapp.Models;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Perms = Imdeliceapp.Services.Perms;

namespace Imdeliceapp.Pages;

[QueryProperty(nameof(ProductIdQuery), "productId")]
[QueryProperty(nameof(ItemIdQuery), "itemId")]
public partial class InventoryMovementsPage : ContentPage
{
    readonly ProductsApi _productsApi = new();
    readonly InventoryApi _inventoryApi = new();

    readonly ObservableCollection<InventoryMovementVm> _movementHistory = new();
    InventoryProductSummary? _selectedProduct;
    int? _requestedProductId;
    int? _requestedItemId;
    int? _selectedItemId;

    public ObservableCollection<InventoryMovementVm> MovementHistory => _movementHistory;

    public string? ProductIdQuery
    {
        get => _requestedProductId?.ToString();
        set
        {
            if (int.TryParse(value, out var id))
                _requestedProductId = id;
        }
    }

    public string? ItemIdQuery
    {
        get => _requestedItemId?.ToString();
        set
        {
            if (int.TryParse(value, out var id))
                _requestedItemId = id;
        }
    }

    public InventoryMovementsPage()
    {
        InitializeComponent();
        BindingContext = this;
        MovementsView.ItemsSource = MovementHistory;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!Perms.InventoryRead)
        {
            await DisplayAlert("Acceso restringido", "No puedes ver movimientos de inventario.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }
        if (_requestedItemId.HasValue)
        {
            await LoadItemAsync(_requestedItemId.Value);
            _requestedItemId = null;
        }
        else if (_requestedProductId.HasValue)
        {
            await LoadProductAsync(_requestedProductId.Value);
            _requestedProductId = null;
        }
        else if (MovementHistory.Count == 0)
        {
            await LoadRecentMovementsAsync();
        }
    }

    async void PickProductButton_Clicked(object sender, EventArgs e)
    {
        if (!Perms.InventoryRead)
        {
            await DisplayAlert("Acceso restringido", "No puedes seleccionar productos.", "OK");
            return;
        }
        bool Filter(ProductPickerPage.ProductDTO p) =>
            !string.IsNullOrWhiteSpace(p.barcode);

        var result = await ProductPickerPage.PickAsync(Navigation, Filter);
        if (result == null) return;
        // Si existe un item de inventario para ese producto, usar el itemId directo.
        var items = await _inventoryApi.ListItemsAsync(productId: result.id);
        var item = items
            .OrderByDescending(i => i.lastMovementAt)
            .FirstOrDefault() ?? items.FirstOrDefault();

        if (item != null)
        {
            await LoadItemAsync(item.id);
        }
        else
        {
            await LoadProductAsync(result.id);
        }
    }

    async void MovementsRefresh_Refreshing(object sender, EventArgs e)
    {
        if (!Perms.InventoryRead)
        {
            MovementsRefresh.IsRefreshing = false;
            return;
        }
        if (_selectedItemId.HasValue)
            await LoadItemMovementsAsync(_selectedItemId.Value);
        else
            await LoadRecentMovementsAsync(_selectedProduct?.ProductId);
        MovementsRefresh.IsRefreshing = false;
    }

    async Task LoadItemAsync(int itemId)
    {
        try
        {
            var item = await _inventoryApi.GetItemAsync(itemId);
            if (item == null)
            {
                await DisplayAlert("Inventario", "No encontramos el insumo.", "OK");
                return;
            }

            _selectedItemId = itemId;
            var prod = item.product;
            var displayName = prod?.name ?? $"Producto #{item.productId}";
            var metaParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(prod?.sku))
                metaParts.Add($"SKU {prod!.sku}");
            if (!string.IsNullOrWhiteSpace(item.unit))
                metaParts.Add(item.unit);

            _selectedProduct = new InventoryProductSummary
            {
                ProductId = item.productId,
                DisplayName = displayName,
                Meta = string.Join(" · ", metaParts)
            };

            SelectedProductLabel.Text = _selectedProduct.DisplayName;
            SelectedProductMetaLabel.Text = _selectedProduct.Meta;

            await LoadItemMovementsAsync(itemId);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Cargar insumo");
        }
    }

    async Task LoadProductAsync(int productId)
    {
        try
        {
            _selectedItemId = null;
            var detail = await _productsApi.GetProductAsync(productId);
            if (detail == null)
            {
                await DisplayAlert("Inventario", "No encontramos el producto.", "OK");
                return;
            }

            _selectedProduct = InventoryProductSummary.From(detail);
            SelectedProductLabel.Text = _selectedProduct.DisplayName;
            SelectedProductMetaLabel.Text = _selectedProduct.Meta;

            await LoadRecentMovementsAsync(productId);
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlert("Inventario", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Cargar producto");
        }
    }

    async Task LoadRecentMovementsAsync(int? productId = null)
    {
        try
        {
            MovementHistory.Clear();
            if (!productId.HasValue)
                _selectedItemId = null;
            List<InventoryMovementDTO> list;
            if (productId.HasValue)
            {
                list = await FetchProductMovementsAsync(productId.Value, 40);
                if (_selectedProduct != null)
                {
                    SelectedProductLabel.Text = _selectedProduct.DisplayName;
                    SelectedProductMetaLabel.Text = list.Count == 0
                        ? "Sin movimientos registrados."
                        : $"Últimos {list.Count} movimientos.";
                }
            }
            else
            {
                var items = await _inventoryApi.ListItemsAsync();
                var recentItems = items
                    .Where(i => i.lastMovementAt.HasValue)
                    .OrderByDescending(i => i.lastMovementAt)
                    .Take(10)
                    .ToList();

                var tasks = recentItems.Select(async item =>
                {
                    var moves = await _inventoryApi.GetItemMovementsAsync(item.id, 5);
                    if (item.product != null)
                    {
                        foreach (var mv in moves)
                            mv.product ??= item.product;
                    }
                    return moves;
                });

                var results = await Task.WhenAll(tasks);
                list = results.SelectMany(m => m)
                    .OrderByDescending(m => m.createdAt)
                    .Take(40)
                    .ToList();

                SelectedProductLabel.Text = "Todos los productos";
                SelectedProductMetaLabel.Text = list.Count == 0
                    ? "Sin movimientos recientes."
                    : $"Últimos {list.Count} movimientos generales.";
            }

            foreach (var mv in list)
                MovementHistory.Add(InventoryMovementVm.From(mv, includeProduct: !productId.HasValue));
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Movimientos");
        }
    }

    async Task<List<InventoryMovementDTO>> FetchProductMovementsAsync(int productId, int limit)
    {
        var result = new List<InventoryMovementDTO>();
        try
        {
            var items = await _inventoryApi.ListItemsAsync(productId: productId);
            var tasks = items.Select(item => _inventoryApi.GetItemMovementsAsync(item.id, limit));
            var perItem = await Task.WhenAll(tasks);
            result = perItem.SelectMany(m => m)
                .OrderByDescending(m => m.createdAt)
                .Take(limit)
                .ToList();
        }
        catch (HttpRequestException ex)
        {
            await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(
                new HttpResponseMessage(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError),
                ex.Message));
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Movimientos por producto");
        }

        return result;
    }

    async Task LoadItemMovementsAsync(int itemId)
    {
        try
        {
            MovementHistory.Clear();
            var list = await _inventoryApi.GetItemMovementsAsync(itemId, 50);
            foreach (var mv in list.OrderByDescending(m => m.createdAt))
                MovementHistory.Add(InventoryMovementVm.From(mv, includeProduct: false));

            SelectedProductMetaLabel.Text = list.Count == 0
                ? "Sin movimientos registrados."
                : $"Últimos {list.Count} movimientos.";
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Movimientos del insumo");
        }
    }

    async void FabButton_Clicked(object sender, EventArgs e)
    {
        if (!Perms.InventoryAdjust)
        {
            await DisplayAlert("Acceso restringido", "No puedes registrar movimientos.", "OK");
            return;
        }
        var option = await DisplayActionSheet("Registrar movimiento", "Cancelar", null,
            "Escanear con pistola", "Registrar manualmente");

        switch (option)
        {
            case "Escanear con pistola":
                await Shell.Current.GoToAsync(nameof(InventoryScannerPage));
                break;
            case "Registrar manualmente":
                await Shell.Current.GoToAsync(nameof(InventoryManualMovementPage));
                break;
        }
    }
}

public class InventoryProductSummary
{
    public int ProductId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Meta { get; init; } = string.Empty;

    public static InventoryProductSummary From(ProductsApi.ProductDetailDTO dto)
    {
        var parts = new[]
        {
            dto.type,
            string.IsNullOrWhiteSpace(dto.sku) ? null : $"SKU {dto.sku}"
        }.Where(p => !string.IsNullOrWhiteSpace(p));

        return new InventoryProductSummary
        {
            ProductId = dto.id,
            DisplayName = dto.name ?? $"Producto #{dto.id}",
            Meta = string.Join(" · ", parts)
        };
    }
}

public class InventoryMovementVm
{
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string QuantityLabel { get; init; } = string.Empty;
    public string Meta { get; init; } = string.Empty;
    public string Badge { get; init; } = string.Empty;

    public static InventoryMovementVm From(InventoryMovementDTO dto, bool includeProduct = false)
    {
        var badge = TranslateType(dto.type);
        var title = includeProduct
            ? dto.product?.name ?? $"Producto #{dto.productId}"
            : badge;
        var subtitle = includeProduct
            ? badge
            : dto.product?.name ?? string.Empty;

        var quantityUnit = TranslateUnit(dto.unit) ?? dto.unit ?? string.Empty;
        var meta = new List<string>();
        if (dto.location != null)
            meta.Add(dto.location.name);
        meta.Add(dto.createdAt.ToLocalTime().ToString("g"));
        if (!string.IsNullOrWhiteSpace(dto.reason))
            meta.Add(dto.reason);

        return new InventoryMovementVm
        {
            Title = title,
            Subtitle = subtitle,
            QuantityLabel = $"{dto.quantity:0.##} {quantityUnit}",
            Meta = string.Join(" · ", meta),
            Badge = badge
        };
    }

    static string TranslateType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return "Movimiento";

        return type.ToUpperInvariant() switch
        {
            "PURCHASE" => "Entrada (Compra)",
            "SALE" => "Salida (Venta)",
            "SALE_RETURN" => "Devolución",
            "WASTE" => "Merma",
            "ADJUSTMENT" => "Ajuste",
            _ => type
        };
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
}
