using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;

namespace Imdeliceapp.Pages;

public partial class ExpensesPage : ContentPage
{
    readonly ExpensesApi _api = new();
    DateTime _fromDate = DateTime.Today;
    DateTime _toDate = DateTime.Today;
    string _search = string.Empty;
    readonly int _tzOffsetMinutes = (int)Math.Round(TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalMinutes);
    bool _isLoading;

    public ObservableCollection<ExpenseVm> Expenses { get; } = new();

    public ExpensesPage()
    {
        InitializeComponent();
        BindingContext = this;
        FromDatePicker.Date = _fromDate;
        ToDatePicker.Date = _toDate;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AddButton.IsVisible = Perms.ExpensesManage;
        _ = LoadAsync();
    }

    async Task LoadAsync()
    {
        if (_isLoading) return;
        try
        {
            _isLoading = true;
            ExpensesRefresh.IsRefreshing = true;

            var list = await _api.ListAsync(_fromDate, _toDate, null, _search, _tzOffsetMinutes);
            Expenses.Clear();
            foreach (var dto in list.OrderByDescending(e => e.incurredAt))
                Expenses.Add(ExpenseVm.From(dto, _tzOffsetMinutes));

            var summary = await _api.GetSummaryAsync(_fromDate, _toDate, null, _tzOffsetMinutes);
            SummaryTotalLabel.Text = FormatAmount(summary.totalAmountCents);
            SummaryInfoLabel.Text = _fromDate == _toDate
                ? $"Hoy {_fromDate:dd MMM}"
                : $"Del {_fromDate:dd MMM} al {_toDate:dd MMM}";
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlert("Gastos", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Gastos", $"No pudimos cargar los gastos: {ex.Message}", "OK");
        }
        finally
        {
            ExpensesRefresh.IsRefreshing = false;
            _isLoading = false;
        }
    }

    static string FormatAmount(int amountCents)
    {
        var amount = amountCents / 100m;
        return amount.ToString("$#,##0.00", CultureInfo.CurrentCulture);
    }

    void FromDatePicker_DateSelected(object sender, DateChangedEventArgs e)
    {
        _fromDate = e.NewDate.Date;
        if (_fromDate > _toDate)
            _toDate = _fromDate;
        ToDatePicker.Date = _toDate;
        _ = LoadAsync();
    }

    void ToDatePicker_DateSelected(object sender, DateChangedEventArgs e)
    {
        _toDate = e.NewDate.Date;
        if (_toDate < _fromDate)
            _fromDate = _toDate;
        FromDatePicker.Date = _fromDate;
        _ = LoadAsync();
    }

    void SearchBar_SearchButtonPressed(object sender, EventArgs e)
    {
        _search = SearchBar.Text?.Trim() ?? string.Empty;
        _ = LoadAsync();
    }

    void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            _search = string.Empty;
            _ = LoadAsync();
        }
    }

    void ExpensesRefresh_Refreshing(object sender, EventArgs e)
    {
        _ = LoadAsync();
    }

    async void AddButton_Clicked(object sender, EventArgs e)
    {
        if (!Perms.ExpensesManage)
        {
            await DisplayAlert("Permisos", "No puedes registrar gastos.", "OK");
            return;
        }
        await Shell.Current.GoToAsync(nameof(ExpenseEditorPage));
    }
}

public class ExpenseVm
{
    public string Concept { get; init; } = string.Empty;
    public string CategoryLabel { get; init; } = string.Empty;
    public string AmountDisplay { get; init; } = string.Empty;
    public string DateLabel { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
    public string PaymentLabel { get; init; } = string.Empty;
    public bool HasPayment => !string.IsNullOrWhiteSpace(PaymentLabel);

    public static ExpenseVm From(ExpensesApi.ExpenseDTO dto, int tzOffsetMinutes)
    {
        var localDate = DateTime.SpecifyKind(dto.incurredAt, DateTimeKind.Utc)
            .AddMinutes(tzOffsetMinutes);
        return new ExpenseVm
        {
            Concept = dto.concept,
            CategoryLabel = TranslateCategory(dto.category),
            AmountDisplay = (dto.amountCents / 100m)
                .ToString("$#,##0.00", CultureInfo.CurrentCulture),
            DateLabel = $"Registrado {localDate:dd MMM yyyy HH:mm}",
            Notes = dto.notes ?? string.Empty,
            PaymentLabel = dto.paymentMethod.HasValue
                ? $"Pago: {TranslatePayment(dto.paymentMethod.Value)}"
                : string.Empty
        };
    }

    static string TranslateCategory(ExpensesApi.ExpenseCategory category) => category switch
    {
        ExpensesApi.ExpenseCategory.SUPPLIES => "Compra urgente",
        ExpensesApi.ExpenseCategory.MAINTENANCE => "Mantenimiento",
        ExpensesApi.ExpenseCategory.LOSS => "Merma/Pérdida",
        _ => "Otro egreso"
    };

    static string TranslatePayment(ExpensesApi.PaymentMethod method) => method switch
    {
        ExpensesApi.PaymentMethod.CASH => "Efectivo",
        ExpensesApi.PaymentMethod.CARD => "Tarjeta",
        ExpensesApi.PaymentMethod.TRANSFER => "Transferencia",
        _ => "Otro"
    };
}
