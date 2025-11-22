using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Imdeliceapp.Helpers;
using Imdeliceapp.Models;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Perms = Imdeliceapp.Services.Perms;

namespace Imdeliceapp.Pages;

public partial class InventoryManualMovementPage : ContentPage
{
    readonly ProductsApi _productsApi = new();
    readonly InventoryApi _inventoryApi = new();

    List<InventoryLocationDTO> _locations = new();
    ProductsApi.ProductDetailDTO? _selectedProduct;

    public InventoryManualMovementPage()
    {
        InitializeComponent();
        TypePicker.ItemsSource = new ObservableCollection<MovementOption>(MovementOption.All);
        TypePicker.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!Perms.InventoryAdjust)
        {
            await DisplayAlert("Acceso restringido", "No puedes registrar movimientos de inventario.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }
        if (_locations.Count == 0)
            await LoadLocationsAsync();
    }

    async Task LoadLocationsAsync()
    {
        try
        {
            _locations = await _inventoryApi.ListLocationsAsync();
            LocationPicker.ItemsSource = _locations;
            var preferred = _locations.FirstOrDefault(l => l.isDefault) ?? _locations.FirstOrDefault();
            if (preferred != null)
                LocationPicker.SelectedItem = preferred;
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Cargar ubicaciones");
        }
    }

    async void PickProductButton_Clicked(object sender, EventArgs e)
    {
        if (!Perms.InventoryRead)
        {
            await DisplayAlert("Acceso restringido", "No puedes seleccionar productos.", "OK");
            return;
        }
        var picked = await ProductPickerPage.PickAsync(Navigation);
        if (picked == null) return;

        await LoadProductAsync(picked.id);
    }

    async Task LoadProductAsync(int id)
    {
        try
        {
            var detail = await _productsApi.GetProductAsync(id);
            if (detail == null)
            {
                ShowStatus("No encontramos el producto.");
                return;
            }

            _selectedProduct = detail;
            ProductNameLabel.Text = detail.name ?? $"Producto #{detail.id}";
            ProductDetailsLabel.Text = $"{detail.type} · SKU {detail.sku}";
            ShowStatus(string.Empty, false);
        }
        catch (HttpRequestException ex)
        {
            ShowStatus(ex.Message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Seleccionar producto");
            ShowStatus("Ocurrió un error.");
        }
    }

    async void RegisterButton_Clicked(object sender, EventArgs e)
    {
        if (_selectedProduct == null)
        {
            ShowStatus("Selecciona un producto.");
            return;
        }

        if (TypePicker.SelectedItem is not MovementOption option)
        {
            ShowStatus("Elige el tipo de movimiento.");
            return;
        }

        if (!decimal.TryParse(QuantityEntry.Text?.Trim(), out var qty) || qty <= 0)
        {
            ShowStatus("Ingresa una cantidad válida.");
            return;
        }

        var location = LocationPicker.SelectedItem as InventoryLocationDTO;

        try
        {
            var dto = new InventoryMovementRequest
            {
                productId = _selectedProduct.id,
                type = option.Value,
                quantity = InventoryMovementHelper.NormalizeQuantity(option.Value, qty),
                locationId = location?.id,
                reason = string.IsNullOrWhiteSpace(ReasonEditor.Text) ? null : ReasonEditor.Text.Trim()
            };

            await _inventoryApi.CreateMovementAsync(dto);
            ShowStatus("Movimiento registrado.", false);
            QuantityEntry.Text = string.Empty;
            ReasonEditor.Text = string.Empty;
        }
        catch (HttpRequestException ex)
        {
            ShowStatus(ex.Message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Movimiento manual");
            ShowStatus("No se pudo registrar el movimiento.");
        }
    }

    void ShowStatus(string message, bool isError = true)
    {
        StatusLabel.Text = message;
        StatusLabel.TextColor = isError ? Color.FromArgb("#B71C1C") : Color.FromArgb("#2E7D32");
        StatusLabel.IsVisible = !string.IsNullOrWhiteSpace(message);
    }
}
