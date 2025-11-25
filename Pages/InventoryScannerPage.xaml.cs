using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Imdeliceapp.Helpers;
using Imdeliceapp.Models;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using ProductEditorPage = Imdeliceapp.Pages.ProductEditorPage;
using Perms = Imdeliceapp.Services.Perms;

namespace Imdeliceapp.Pages;

public partial class InventoryScannerPage : ContentPage
{
    readonly ProductsApi _productsApi = new();
    readonly InventoryApi _inventoryApi = new();

    public ObservableCollection<PendingScanVm> PendingScans { get; } = new();
    public List<MovementOption> MovementOptions { get; } = MovementOption.All.ToList();

    public Command<PendingScanVm> RemoveCommand { get; }

    string _statusMessage = string.Empty;
    Color _statusColor = Colors.Red;
    MovementOption? _defaultMovement;
    string? _pendingRetryBarcode;

    public bool HasStatus => !string.IsNullOrWhiteSpace(_statusMessage);
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage == value) return;
            _statusMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStatus));
        }
    }
    public Color StatusColor
    {
        get => _statusColor;
        set
        {
            if (_statusColor == value) return;
            _statusColor = value;
            OnPropertyChanged();
        }
    }

    public InventoryScannerPage()
    {
        InitializeComponent();
        BindingContext = this;
        RemoveCommand = new Command<PendingScanVm>(vm =>
        {
            if (vm != null && PendingScans.Contains(vm))
                PendingScans.Remove(vm);
        });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!Perms.InventoryAdjust)
        {
            Dispatcher.Dispatch(async () =>
            {
                await DisplayAlert("Acceso restringido", "No puedes registrar movimientos de inventario.", "OK");
                await Shell.Current.GoToAsync("..");
            });
            return;
        }
        Dispatcher.Dispatch(async () =>
        {
            FocusScannerEntry();

            if (_defaultMovement == null)
            {
                var selected = await AskDefaultMovementAsync();
                if (!selected)
                {
                    await Shell.Current.GoToAsync("..");
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(_pendingRetryBarcode))
            {
                var retry = _pendingRetryBarcode;
                _pendingRetryBarcode = null;
                await TryAddBarcodeAsync(retry!, allowProductCreationPrompt: false);
            }
        });
    }

    void BarcodeEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Mantener foco cuando la pistola envía Enter
        FocusScannerEntry();
    }

    async void BarcodeEntry_Completed(object sender, EventArgs e)
    {
        var code = BarcodeEntry.Text?.Trim();
        FocusScannerEntry();
        if (string.IsNullOrWhiteSpace(code))
            return;

        await TryAddBarcodeAsync(code);
    }

    void PendingList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Evita que el CollectionView capture el foco del escáner
        if (PendingList.SelectedItem != null)
            PendingList.SelectedItem = null;
        FocusScannerEntry();
    }

    async void SendButton_Clicked(object sender, EventArgs e)
    {
        if (PendingScans.Count == 0)
        {
            ShowStatus("No hay lecturas para enviar.");
            return;
        }

        try
        {
            foreach (var item in PendingScans)
            {
                var dto = new InventoryMovementByBarcodeRequest
                {
                    barcode = item.Barcode,
                    type = item.Type,
                    quantity = InventoryMovementHelper.NormalizeQuantity(item.Type, item.Quantity),
                    locationId = null,
                    reason = item.Reason
                };
                await _inventoryApi.CreateMovementByBarcodeAsync(dto);
            }

            ShowStatus("Movimientos registrados.", false);
            PendingScans.Clear();
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var missingBarcode = PendingScans.FirstOrDefault()?.Barcode ?? "desconocido";
                ShowStatus($"No encontramos el producto ({missingBarcode}). Regístralo antes de enviar.", true);
            }
            else
            {
                ShowStatus(ParseApiError(ex.Message));
            }
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Registrar lecturas");
            ShowStatus("No se pudieron registrar las lecturas.");
        }
    }

    void FocusScannerEntry()
    {
        // Forzar el foco al campo de escaneo y limpiar selección de la lista
        Dispatcher.Dispatch(() =>
        {
            if (!BarcodeEntry.IsFocused)
                BarcodeEntry.Focus();
            // Asegura que ningún elemento quede seleccionado y reciba foco
            if (PendingList != null)
                PendingList.SelectedItem = null;
        });
    }

    void ClearButton_Clicked(object sender, EventArgs e)
    {
        PendingScans.Clear();
        ShowStatus("Lecturas eliminadas.", false);
    }

    void ShowStatus(string message, bool isError = true)
    {
        StatusMessage = message;
        StatusColor = isError ? Color.FromArgb("#B71C1C") : Color.FromArgb("#2E7D32");
    }

    async Task TryAddBarcodeAsync(string? code, bool allowProductCreationPrompt = true)
    {
        var trimmed = code?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return;

        try
        {
            var product = await _productsApi.GetByBarcodeAsync(trimmed);
            if (product == null)
            {
                if (allowProductCreationPrompt)
                    await OfferCreateProductAsync(trimmed);
                else
                    ShowStatus($"No encontramos el código {trimmed}.");
                return;
            }

            var normalizedCode = product.barcode ?? product.id.ToString();
            var existing = PendingScans.FirstOrDefault(p =>
                string.Equals(p.Barcode, normalizedCode, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.Quantity += 1;
                ShowStatus($"Actualizado {existing.DisplayName}: {existing.Quantity}", false);
            }
            else
            {
                var vm = PendingScanVm.From(product);
                if (_defaultMovement != null)
                    vm.TypeIndex = MovementOptions.IndexOf(_defaultMovement);
                PendingScans.Add(vm);
                ShowStatus($"Agregado {product.name ?? product.barcode}", false);
            }
        }
        catch (HttpRequestException ex)
        {
// #if DEBUG
//             await DisplayAlert("Depuración API",
//                 $"Status: {ex.StatusCode?.ToString() ?? "n/a"}\nMensaje: {ex.Message}",
//                 "OK");
// #endif
            var parsed = ParseApiError(ex.Message);
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                if (allowProductCreationPrompt)
                    await OfferCreateProductAsync(trimmed);
                else
                    ShowStatus($"No encontramos el código {trimmed}.");
            }
            else
            {
                ShowStatus(parsed);
            }
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Escáner");
            ShowStatus("Ocurrió un error.", true);
        }
        finally
        {
            BarcodeEntry.Text = string.Empty;
            BarcodeEntry.Focus();
        }
    }

    async Task OfferCreateProductAsync(string barcode)
    {
        var option = await DisplayActionSheet(
            $"No encontramos el código {barcode}. ¿Qué deseas hacer?",
            "Cancelar",
            null,
            "Asignar a producto existente",
            "Asignar a insumo existente",
            "Crear insumo nuevo",
            "Crear producto nuevo");

        switch (option)
        {
            case null:
            case "Cancelar":
                ShowStatus($"No encontramos el código {barcode}.");
                return;

            case "Asignar a producto existente":
                await PickExistingAndEditAsync(barcode);
                break;

            case "Asignar a insumo existente":
                await PickExistingInsumoAndEditAsync(barcode);
                break;

            case "Crear insumo nuevo":
                _pendingRetryBarcode = barcode;
                await Shell.Current.GoToAsync($"{nameof(InventoryInsumoEditorPage)}?initialBarcode={Uri.EscapeDataString(barcode)}");
                ShowStatus("Captura el insumo y regresa para continuar.", false);
                break;

            case "Crear producto nuevo":
                _pendingRetryBarcode = barcode;
                await Shell.Current.GoToAsync($"{nameof(ProductEditorPage)}?mode=create&initialBarcode={Uri.EscapeDataString(barcode)}");
                ShowStatus("Captura el producto y regresa para continuar.", false);
                break;
        }
    }

    async Task PickExistingAndEditAsync(string barcode)
    {
        try
        {
            // Solo mostrar productos sin código de barras para asignarles este
            bool Filter(ProductPickerPage.ProductDTO p) =>
                string.IsNullOrWhiteSpace(p.barcode);

            var chosen = await ProductPickerPage.PickAsync(Navigation, Filter);
            if (chosen == null) return;

            _pendingRetryBarcode = barcode;

            var items = await _inventoryApi.ListItemsAsync(productId: chosen.id);
            var isInventoryCategory = items.FirstOrDefault()?.product?.categorySlug?.Contains("inventario", StringComparison.OrdinalIgnoreCase) == true
                                      || items.FirstOrDefault()?.product?.categoryName?.Contains("inventario", StringComparison.OrdinalIgnoreCase) == true;

            if (isInventoryCategory)
            {
                await Shell.Current.GoToAsync(
                    $"{nameof(InventoryInsumoEditorPage)}?productId={chosen.id}&initialBarcode={Uri.EscapeDataString(barcode)}");
            }
            else
            {
                await Shell.Current.GoToAsync(
                    $"{nameof(ProductEditorPage)}?mode=edit&id={chosen.id}&initialBarcode={Uri.EscapeDataString(barcode)}");
            }

            ShowStatus("Asigna el código y guarda; al volver se reintentará el escaneo.", false);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Asignar código");
        }
    }

    async Task PickExistingInsumoAndEditAsync(string barcode)
    {
        try
        {
            var items = await _inventoryApi.ListItemsAsync();
            var insumos = items
                .Where(i => (i.product?.categorySlug ?? i.product?.categoryName ?? string.Empty)
                    .Contains("inventario", StringComparison.OrdinalIgnoreCase))
                .OrderBy(i => i.product?.name ?? $"Producto #{i.productId}")
                .ToList();

            if (insumos.Count == 0)
            {
                await DisplayAlert("Inventario", "No hay insumos para asignar código. Crea uno nuevo.", "OK");
                return;
            }

            var names = insumos.Select(i => i.product?.name ?? $"Producto #{i.productId}").ToList();
            var choice = await DisplayActionSheet("Seleccione el insumo", "Cancelar", null, names.ToArray());
            if (string.IsNullOrWhiteSpace(choice) || choice == "Cancelar") return;

            var selected = insumos.FirstOrDefault(i => (i.product?.name ?? $"Producto #{i.productId}") == choice);
            if (selected == null) return;

            _pendingRetryBarcode = barcode;
            await Shell.Current.GoToAsync(
                $"{nameof(InventoryInsumoEditorPage)}?productId={selected.productId}&initialBarcode={Uri.EscapeDataString(barcode)}");
            ShowStatus("Asigna el código y guarda; al volver se reintentará el escaneo.", false);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Asignar a insumo existente");
        }
    }

    async Task<bool> AskDefaultMovementAsync()
    {
        var labels = MovementOptions.Select(m => m.Label).ToArray();
        var choice = await DisplayActionSheet("¿Qué tipo de movimiento registrarás?", "Cancelar", null, labels);
        if (choice == null || choice == "Cancelar")
            return false;

        var selected = MovementOptions.FirstOrDefault(opt => opt.Label == choice);
        _defaultMovement = selected ?? MovementOptions.First();
        ShowStatus($"Usando {_defaultMovement.Label} por defecto.", false);
        return true;
    }
    static string ParseApiError(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "No se pudo completar la operación.";

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("message", out var msg))
                return msg.GetString() ?? raw;
            if (doc.RootElement.TryGetProperty("error", out var err))
                return err.GetString() ?? raw;
        }
        catch
        {
            // cuerpo no era JSON
        }

        return raw;
    }
}

public class PendingScanVm : BindableObject
{
    int _typeIndex;
    decimal _quantity = 1;
    string? _reason;

    public string Barcode { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;

    public int TypeIndex
    {
        get => _typeIndex;
        set
        {
            if (_typeIndex == value) return;
            _typeIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Type));
        }
    }

    public string Type => MovementOption.All[Math.Clamp(_typeIndex, 0, MovementOption.All.Count - 1)].Value;

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (value <= 0) value = 1;
            if (_quantity == value) return;
            _quantity = value;
            OnPropertyChanged();
        }
    }

    public string? Reason
    {
        get => _reason;
        set
        {
            if (_reason == value) return;
            _reason = value;
            OnPropertyChanged();
        }
    }

    public static PendingScanVm From(ProductsApi.ProductBarcodeDTO dto)
    {
        var barcode = dto.barcode ?? dto.id.ToString();
        return new PendingScanVm
        {
            Barcode = barcode,
            DisplayName = dto.name ?? $"Producto #{dto.id}",
            TypeIndex = 0
        };
    }
}

public record MovementOption(string Value, string Label)
{
    public static readonly IReadOnlyList<MovementOption> All = new[]
    {
        new MovementOption("PURCHASE", "Entrada (Compra/Ajuste)"),
        new MovementOption("SALE", "Salida (Venta)"),
        new MovementOption("SALE_RETURN", "Devolución de venta"),
        new MovementOption("WASTE", "Merma"),
        new MovementOption("ADJUSTMENT", "Ajuste manual"),
    };
}
