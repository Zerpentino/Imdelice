using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Imdeliceapp.Models;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;

namespace Imdeliceapp.Pages;

public partial class ProfitLossPage : ContentPage
{
    readonly ReportsApi _reportsApi = new();
    readonly ExpensesApi _expensesApi = new();
    readonly OrdersApi _ordersApi = new();
    readonly int _tzOffsetMinutes = (int)Math.Round(TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalMinutes);
    DateTime _fromDate = DateTime.Today;
    DateTime _toDate = DateTime.Today;
    bool _isLoading;

    public ObservableCollection<PaymentMethodVm> PaymentSummary { get; } = new();
    public ObservableCollection<CategoryExpenseVm> ExpenseSummary { get; } = new();
    public ObservableCollection<OrderVm> Orders { get; } = new();
    public ObservableCollection<ExpenseDetailVm> ExpensesDetail { get; } = new();

    public ProfitLossPage()
    {
        InitializeComponent();
        PaymentsCollection.ItemsSource = PaymentSummary;
        ExpensesCollection.ItemsSource = ExpenseSummary;
        OrdersCollection.ItemsSource = Orders;
        ExpensesDetailCollection.ItemsSource = ExpensesDetail;
        FromDatePicker.Date = _fromDate;
        ToDatePicker.Date = _toDate;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadAsync();
    }

    async Task LoadAsync()
    {
        if (_isLoading) return;
        try
        {
            _isLoading = true;
            ReportRefresh.IsRefreshing = true;

            var report = await _reportsApi.GetProfitLossAsync(_fromDate, _toDate, _tzOffsetMinutes);
            var incomeCents = report.payments?.grandTotals?.amountCents ?? 0;
            IncomeLabel.Text = FormatAmount(incomeCents);
            ExpensesLabel.Text = FormatAmount(report.expenses.totalAmountCents);
            NetLabel.Text = FormatAmount(report.netAmountCents);
            NetLabel.TextColor = report.netAmountCents >= 0
                ? Color.FromArgb("#1B5E20")
                : Color.FromArgb("#B71C1C");

            PaymentSummary.Clear();
            foreach (var item in report.payments.totalsByMethod.OrderByDescending(p => p.amountCents))
                PaymentSummary.Add(PaymentMethodVm.From(item));

            ExpenseSummary.Clear();
            foreach (var item in report.expenses.totalsByCategory.OrderByDescending(c => c.amountCents))
                ExpenseSummary.Add(CategoryExpenseVm.From(item));

            IncomeSummaryLabel.Text = PaymentSummary.Count > 0
                ? "Ingresos: " + string.Join(" · ", PaymentSummary.Select(p => $"{p.Method}: {p.AmountDisplay}"))
                : "Ingresos: sin registros en el periodo.";

            ExpenseSummaryLabel.Text = ExpenseSummary.Count > 0
                ? "Gastos: " + string.Join(" · ", ExpenseSummary.Select(e => $"{e.Category}: {e.AmountDisplay}"))
                : "Gastos: sin registros en el periodo.";

            Orders.Clear();
            foreach (var ord in report.payments.orders.OrderByDescending(o => o.closedAt ?? o.openedAt ?? DateTime.MinValue))
                Orders.Add(OrderVm.From(ord, _tzOffsetMinutes));
            if (Orders.Count == 0 && (report.payments?.grandTotals?.amountCents ?? 0) > 0)
            {
                var fallbackOrders = await _ordersApi.ListAsync(new OrderListQuery
                {
                    from = _fromDate,
                    to = _toDate,
                    tzOffsetMinutes = _tzOffsetMinutes,
                    statuses = "CLOSED"
                });
                foreach (var ord in fallbackOrders.OrderByDescending(o => o.closedAt ?? o.openedAt ?? DateTime.MinValue))
                    Orders.Add(OrderVm.From(ord, _tzOffsetMinutes));
            }
            OrdersCollection.IsVisible = Orders.Count > 0;
            OrdersEmptyLabel.IsVisible = Orders.Count == 0;

            ExpensesDetail.Clear();
            var expensesList = await _expensesApi.ListAsync(_fromDate, _toDate, null, null, _tzOffsetMinutes);
            foreach (var exp in expensesList.OrderByDescending(e => e.incurredAt))
                ExpensesDetail.Add(ExpenseDetailVm.From(exp, _tzOffsetMinutes));
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlert("Ganancias vs Gastos", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ganancias vs Gastos", $"No pudimos cargar el reporte: {ex.Message}", "OK");
        }
        finally
        {
            _isLoading = false;
            ReportRefresh.IsRefreshing = false;
        }
    }

    void FromDatePicker_DateSelected(object sender, DateChangedEventArgs e)
    {
        _fromDate = e.NewDate.Date;
        if (_fromDate > _toDate)
        {
            _toDate = _fromDate;
            ToDatePicker.Date = _toDate;
        }
    }

    void ToDatePicker_DateSelected(object sender, DateChangedEventArgs e)
    {
        _toDate = e.NewDate.Date;
        if (_toDate < _fromDate)
        {
            _fromDate = _toDate;
            FromDatePicker.Date = _fromDate;
        }
    }

    void RefreshButton_Clicked(object sender, EventArgs e)
    {
        _ = LoadAsync();
    }

    void ReportRefresh_Refreshing(object sender, EventArgs e)
    {
        _ = LoadAsync();
    }

    static string FormatAmount(int amountCents)
    {
        return (amountCents / 100m).ToString("$#,##0.00", CultureInfo.CurrentCulture);
    }
}

public class PaymentMethodVm
{
    public string Method { get; init; } = string.Empty;
    public string AmountDisplay { get; init; } = string.Empty;

    public static PaymentMethodVm From(ReportsApi.PaymentSummaryDTO dto) =>
        new()
        {
            Method = TranslateMethod(dto.method),
            AmountDisplay = (dto.amountCents / 100m).ToString("$#,##0.00", CultureInfo.CurrentCulture)
        };

    static string TranslateMethod(string method) => method?.ToUpperInvariant() switch
    {
        "CASH" => "Efectivo",
        "CARD" => "Tarjeta",
        "TRANSFER" => "Transferencia",
        _ => method ?? "Otro"
    };
}

public class CategoryExpenseVm
{
    public string Category { get; init; } = string.Empty;
    public string AmountDisplay { get; init; } = string.Empty;

    public static CategoryExpenseVm From(ReportsApi.CategoryTotalDTO dto) =>
        new()
        {
            Category = Translate(dto.category),
            AmountDisplay = (dto.amountCents / 100m).ToString("$#,##0.00", CultureInfo.CurrentCulture)
        };

    public static string Translate(ExpensesApi.ExpenseCategory category) => category switch
    {
        ExpensesApi.ExpenseCategory.SUPPLIES => "Compras urgentes",
        ExpensesApi.ExpenseCategory.MAINTENANCE => "Mantenimiento",
        ExpensesApi.ExpenseCategory.LOSS => "Merma/Pérdida",
        _ => "Otros"
    };
}

public class OrderVm
{
    public string Title { get; init; } = string.Empty;
    public string SubTitle { get; init; } = string.Empty;
    public string Meta { get; init; } = string.Empty;
    public string TotalDisplay { get; init; } = string.Empty;

    public static OrderVm From(ReportsApi.OrderEntryDTO dto, int tzOffsetMinutes)
    {
        var closedLocal = dto.closedAt?.ToUniversalTime().AddMinutes(tzOffsetMinutes);
        var openedLocal = dto.openedAt?.ToUniversalTime().AddMinutes(tzOffsetMinutes);
        var when = closedLocal ?? openedLocal;

        var paymentMeta = dto.payments != null && dto.payments.Count > 0
            ? string.Join(" · ", dto.payments.Select(p => $"{p.method}: {(p.amountCents / 100m).ToString("$#,##0.00", CultureInfo.CurrentCulture)}"))
            : "Sin pagos";

        return new OrderVm
        {
            Title = dto.code ?? $"Orden #{dto.orderId}",
            SubTitle = when.HasValue ? $"Cerrada {when:dd MMM yyyy HH:mm}" : "En curso",
            Meta = paymentMeta,
            TotalDisplay = (dto.totalCents / 100m).ToString("$#,##0.00", CultureInfo.CurrentCulture)
        };
    }

    public static OrderVm From(OrderSummaryDTO dto, int tzOffsetMinutes)
    {
        var closedLocal = dto.closedAt?.ToUniversalTime().AddMinutes(tzOffsetMinutes);
        var openedLocal = dto.openedAt?.ToUniversalTime().AddMinutes(tzOffsetMinutes);
        var when = closedLocal ?? openedLocal;

        var paymentMeta = dto.payments != null && dto.payments.Count > 0
            ? string.Join(" · ", dto.payments.Select(p => $"{p.method}: {(p.amountCents / 100m).ToString("$#,##0.00", CultureInfo.CurrentCulture)}"))
            : "Sin pagos";

        return new OrderVm
        {
            Title = string.IsNullOrWhiteSpace(dto.code) ? $"Orden #{dto.id}" : dto.code,
            SubTitle = when.HasValue ? $"Cerrada {when:dd MMM yyyy HH:mm}" : "En curso",
            Meta = paymentMeta,
            TotalDisplay = (dto.totalCents / 100m).ToString("$#,##0.00", CultureInfo.CurrentCulture)
        };
    }
}

public class ExpenseDetailVm
{
    public string Concept { get; init; } = string.Empty;
    public string CategoryLabel { get; init; } = string.Empty;
    public string DateLabel { get; init; } = string.Empty;
    public string AmountDisplay { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

    public static ExpenseDetailVm From(ExpensesApi.ExpenseDTO dto, int tzOffsetMinutes)
    {
        var local = DateTime.SpecifyKind(dto.incurredAt, DateTimeKind.Utc).AddMinutes(tzOffsetMinutes);
        return new ExpenseDetailVm
        {
            Concept = dto.concept,
            CategoryLabel = CategoryExpenseVm.Translate(dto.category),
            DateLabel = local.ToString("dd MMM yyyy HH:mm"),
            AmountDisplay = (dto.amountCents / 100m).ToString("$#,##0.00", CultureInfo.CurrentCulture),
            Notes = dto.notes ?? string.Empty
        };
    }
}
