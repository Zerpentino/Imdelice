using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;

namespace Imdeliceapp.Pages;

public partial class ExpenseEditorPage : ContentPage
{
    readonly ExpensesApi _expensesApi = new();
    readonly List<Option<ExpensesApi.ExpenseCategory>> _categories = ExpenseCategoryOption.All;
    readonly List<Option<ExpensesApi.PaymentMethod>> _payments = PaymentMethodOption.All;
    readonly List<Option<ExpensesApi.InventoryMovementType>> _movementTypes = MovementTypeOption.All;

    Option<ExpensesApi.ExpenseCategory>? _selectedCategory;
    Option<ExpensesApi.PaymentMethod>? _selectedPayment;
    Option<ExpensesApi.InventoryMovementType>? _selectedMovementType;

    int? _movementProductId;
    string? _movementProductName;

    public ExpenseEditorPage()
    {
        InitializeComponent();
        CategoryPicker.ItemsSource = _categories;
        PaymentPicker.ItemsSource = _payments;
        MovementTypePicker.ItemsSource = _movementTypes;
        CategoryPicker.SelectedIndex = -1;
        if (_movementTypes.Count > 0)
        {
            MovementTypePicker.SelectedIndex = 0;
            _selectedMovementType = _movementTypes[0];
        }
        PaymentPicker.SelectedIndex = -1;
    }

    void MovementSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        MovementFields.IsVisible = e.Value;
    }

    void CategoryPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        _selectedCategory = CategoryPicker.SelectedIndex >= 0
            ? _categories[CategoryPicker.SelectedIndex]
            : null;
    }

    void PaymentPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        _selectedPayment = PaymentPicker.SelectedIndex >= 0
            ? _payments[PaymentPicker.SelectedIndex]
            : null;
    }

    void MovementTypePicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        _selectedMovementType = MovementTypePicker.SelectedIndex >= 0
            ? _movementTypes[MovementTypePicker.SelectedIndex]
            : null;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!Perms.ExpensesManage)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Permisos", "No puedes registrar gastos.", "OK");
                await Shell.Current.GoToAsync("..");
            });
            return;
        }
        CategoryPicker.SelectedIndexChanged += CategoryPicker_SelectedIndexChanged;
        PaymentPicker.SelectedIndexChanged += PaymentPicker_SelectedIndexChanged;
        MovementTypePicker.SelectedIndexChanged += MovementTypePicker_SelectedIndexChanged;

        if (PaymentPicker.SelectedIndex < 0)
            PaymentPicker.SelectedIndex = -1;
        if (MovementTypePicker.SelectedIndex < 0 && _movementTypes.Count > 0)
            MovementTypePicker.SelectedIndex = 0;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        CategoryPicker.SelectedIndexChanged -= CategoryPicker_SelectedIndexChanged;
        PaymentPicker.SelectedIndexChanged -= PaymentPicker_SelectedIndexChanged;
        MovementTypePicker.SelectedIndexChanged -= MovementTypePicker_SelectedIndexChanged;
    }

    async void PickProductButton_Clicked(object sender, EventArgs e)
    {
        var product = await ProductPickerPage.PickAsync(Navigation);
        if (product == null) return;
        _movementProductId = product.id;
        _movementProductName = product.name;
        ProductLabel.Text = $"Producto: {_movementProductName}";
    }

    async void SaveButton_Clicked(object sender, EventArgs e)
    {
        await SaveAsync();
    }

    async Task SaveAsync()
    {
        var concept = ConceptEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(concept) || concept.Length < 3)
        {
            await DisplayAlert("Gastos", "Captura un concepto válido (mín. 3 caracteres).", "OK");
            return;
        }

        if (_selectedCategory == null)
        {
            await DisplayAlert("Gastos", "Selecciona una categoría.", "OK");
            return;
        }

        if (!decimal.TryParse(AmountEntry.Text?.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out var amount) || amount <= 0)
        {
            await DisplayAlert("Gastos", "Ingresa un monto válido.", "OK");
            return;
        }

        ExpensesApi.InventoryMovementPayload? movement = null;
        if (MovementSwitch.IsToggled)
        {
            var barcode = BarcodeEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(barcode) && !_movementProductId.HasValue)
            {
                await DisplayAlert("Gastos", "Ingresa un código de barras o selecciona un producto para ajustar inventario.", "OK");
                return;
            }

            if (!double.TryParse(MovementQuantityEntry.Text?.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out var qty) || qty <= 0)
            {
                await DisplayAlert("Gastos", "Indica una cantidad válida para el movimiento de inventario.", "OK");
                return;
            }

            movement = new ExpensesApi.InventoryMovementPayload
            {
                barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode,
                productId = string.IsNullOrWhiteSpace(barcode) ? _movementProductId : null,
                type = _selectedMovementType?.Value ?? ExpensesApi.InventoryMovementType.PURCHASE,
                quantity = qty,
                reason = MovementReasonEntry.Text?.Trim()
            };
        }

        var payload = new ExpensesApi.CreateExpenseRequest
        {
            concept = concept,
            category = _selectedCategory.Value,
            amountCents = (int)Math.Round(amount * 100m),
            paymentMethod = _selectedPayment?.Value,
            notes = NotesEditor.Text?.Trim(),
            incurredAt = null,
            inventoryMovement = movement
        };

        try
        {
            SaveButton.IsEnabled = false;
            await _expensesApi.CreateAsync(payload);
            await DisplayAlert("Gastos", "Gasto registrado correctamente.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlert("Gastos", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Gastos", $"No pudimos guardar el gasto: {ex.Message}", "OK");
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }
}

public record Option<T>(T Value, string Label);

public static class ExpenseCategoryOption
{
    public static readonly List<Option<ExpensesApi.ExpenseCategory>> All = new()
    {
        new(ExpensesApi.ExpenseCategory.SUPPLIES, "Compra urgente"),
        new(ExpensesApi.ExpenseCategory.MAINTENANCE, "Mantenimiento"),
        new(ExpensesApi.ExpenseCategory.LOSS, "Merma/Pérdida"),
        new(ExpensesApi.ExpenseCategory.OTHER, "Otro")
    };
}

public static class PaymentMethodOption
{
    public static readonly List<Option<ExpensesApi.PaymentMethod>> All = new()
    {
        new(ExpensesApi.PaymentMethod.CASH, "Efectivo"),
        new(ExpensesApi.PaymentMethod.CARD, "Tarjeta"),
        new(ExpensesApi.PaymentMethod.TRANSFER, "Transferencia"),
        new(ExpensesApi.PaymentMethod.OTHER, "Otro")
    };
}

public static class MovementTypeOption
{
    public static readonly List<Option<ExpensesApi.InventoryMovementType>> All = new()
    {
        new(ExpensesApi.InventoryMovementType.PURCHASE, "Entrada (Compra)"),
        new(ExpensesApi.InventoryMovementType.ADJUSTMENT, "Ajuste"),
        new(ExpensesApi.InventoryMovementType.WASTE, "Merma"),
        new(ExpensesApi.InventoryMovementType.SALE, "Salida"),
        new(ExpensesApi.InventoryMovementType.SALE_RETURN, "Devolución"),
        new(ExpensesApi.InventoryMovementType.TRANSFER, "Transferencia")
    };
}
