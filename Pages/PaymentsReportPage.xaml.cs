using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Linq;
using Imdeliceapp.Helpers;
using Imdeliceapp.Models;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Graphics;

namespace Imdeliceapp.Pages;

public partial class PaymentsReportPage : ContentPage, INotifyPropertyChanged
{
    readonly OrdersApi _ordersApi = new();

    DateTime _fromDate = DateTime.Today;
    DateTime _toDate = DateTime.Today;
    bool _isLoading;
    string _summaryText = string.Empty;
    string _ordersClosedSummary = string.Empty;
    string _rangeSummary = string.Empty;
    bool _showOrderItems;
    string _ordersToggleIcon = "+";

    public PaymentsReportPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public ObservableCollection<PaymentMethodReportItem> MethodItems { get; } = new();
    public ObservableCollection<PaymentOrderReportItem> OrderItems { get; } = new();

    public DateTime Today => DateTime.Today;

    public DateTime FromDate
    {
        get => _fromDate;
        set
        {
            if (_fromDate == value) return;
            _fromDate = value;
            if (_fromDate > ToDate)
                ToDate = _fromDate;
            RaisePropertyChanged();
        }
    }

    public DateTime ToDate
    {
        get => _toDate;
        set
        {
            if (_toDate == value) return;
            _toDate = value;
            RaisePropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading == value) return;
            _isLoading = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsNotLoading));
            RaisePropertyChanged(nameof(ShowEmptyState));
        }
    }

    public bool IsNotLoading => !IsLoading;

    public string SummaryText
    {
        get => _summaryText;
        set
        {
            if (_summaryText == value) return;
            _summaryText = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasSummary));
        }
    }

    public string OrdersClosedSummary
    {
        get => _ordersClosedSummary;
        set
        {
            if (_ordersClosedSummary == value) return;
            _ordersClosedSummary = value;
            RaisePropertyChanged();
        }
    }

    public string RangeSummary
    {
        get => _rangeSummary;
        set
        {
            if (_rangeSummary == value) return;
            _rangeSummary = value;
            RaisePropertyChanged();
        }
    }

    public bool HasResults => MethodItems.Count > 0;
    public bool HasOrderItems => OrderItems.Count > 0;
    public bool HasSummary => (HasResults || HasOrderItems) && !string.IsNullOrWhiteSpace(SummaryText);
    public bool ShowEmptyState => !IsLoading && !HasResults && !HasOrderItems;
    public bool ShowOrderItems
    {
        get => _showOrderItems;
        set
        {
            if (_showOrderItems == value) return;
            _showOrderItems = value;
            RaisePropertyChanged();
        }
    }

    public string OrdersToggleIcon
    {
        get => _ordersToggleIcon;
        set
        {
            if (_ordersToggleIcon == value) return;
            _ordersToggleIcon = value;
            RaisePropertyChanged();
        }
    }

    public Command ToggleOrdersCommand => new(() =>
    {
        ShowOrderItems = !ShowOrderItems;
        OrdersToggleIcon = ShowOrderItems ? "−" : "+";
    });

    Command<PaymentOrderReportItem>? _openOrderCommand;
    public Command<PaymentOrderReportItem> OpenOrderCommand
        => _openOrderCommand ??= new Command<PaymentOrderReportItem>(async item => await NavigateToOrderAsync(item));

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadReportAsync();
    }

    async void Refresh_Clicked(object sender, EventArgs e) => await LoadReportAsync();

    async Task NavigateToOrderAsync(PaymentOrderReportItem? item)
    {
        if (item == null)
            return;

        try
        {
            await Shell.Current.GoToAsync($"{nameof(OrderDetailPage)}?orderId={item.OrderId}");
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Navegar a detalle de orden");
        }
    }

    async Task LoadReportAsync()
    {
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await ErrorHandler.MostrarErrorUsuario("Sin conexión a Internet.");
                return;
            }

            IsLoading = true;

            var report = await _ordersApi.GetPaymentsReportAsync(
                FromDate,
                ToDate,
                includeOrders: true,
                tzOffsetMinutes: OrdersApi.GetLocalTimezoneOffsetMinutes());

            MethodItems.Clear();
            if (report?.totalsByMethod != null)
            {
                foreach (var item in report.totalsByMethod)
                    MethodItems.Add(PaymentMethodReportItem.From(item));
            }

            RangeSummary = BuildRangeSummary(report?.range?.from, report?.range?.to);
            if (report?.grandTotals != null)
            {
                SummaryText = BuildSummary(report.grandTotals);
                OrdersClosedSummary = $"Cerradas: {report.grandTotals.ordersClosed} · Canceladas: {report.grandTotals.ordersCanceled} · Reembolsadas: {report.grandTotals.ordersRefunded}";
            }
            else
            {
                SummaryText = string.Empty;
                OrdersClosedSummary = string.Empty;
            }

            OrderItems.Clear();
            if (report?.orders != null)
            {
                foreach (var order in report.orders)
                    OrderItems.Add(PaymentOrderReportItem.From(order));
            }
            ShowOrderItems = false;
            OrdersToggleIcon = "+";

            RaisePropertyChanged(nameof(HasResults));
            RaisePropertyChanged(nameof(HasOrderItems));
            RaisePropertyChanged(nameof(ShowEmptyState));
            RaisePropertyChanged(nameof(HasSummary));
        }
        catch (HttpRequestException ex)
        {
            var message = ErrorHandler.ObtenerMensajeHttp(new HttpResponseMessage(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), ex.Message);
            await ErrorHandler.MostrarErrorUsuario(message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Reporte de pagos");
        }
        finally
        {
            IsLoading = false;
        }
    }

    static string BuildSummary(PaymentsReportTotalsDto totals)
    {
        var amount = FormatCurrency(totals.amountCents);
        var tips = FormatCurrency(totals.tipCents);
        var change = FormatCurrency(totals.changeCents);
        var received = FormatCurrency(totals.receivedAmountCents);
        return $"Aplicado: {amount} · Propinas: {tips} · Cambio: {change} · Recibido: {received}";
    }

    static string BuildRangeSummary(DateTime? from, DateTime? to)
    {
        var localFrom = from.HasValue ? ToLocal(from.Value) : (DateTime?)null;
        var localTo = to.HasValue ? ToLocal(to.Value) : (DateTime?)null;

        if (localFrom == null && localTo == null)
            return "Reporte acumulado";
        if (localFrom != null && localTo != null)
            return $"Del {localFrom.Value:dd/MM/yyyy} al {localTo.Value:dd/MM/yyyy}";
        return localFrom != null
            ? $"Desde {localFrom.Value:dd/MM/yyyy}"
            : $"Hasta {localTo!.Value:dd/MM/yyyy}";
    }

    public static string FormatCurrency(int cents) => (cents / 100m).ToString("C", CultureInfo.CurrentCulture);
    public static DateTime ToLocal(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value.ToLocalTime(),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Local),
        _ => value
    };

    public new event PropertyChangedEventHandler? PropertyChanged;
    void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public class PaymentMethodReportItem
    {
        public string MethodDisplay { get; init; } = string.Empty;
        public string AmountSummary { get; init; } = string.Empty;
        public string ExtrasSummary { get; init; } = string.Empty;

        public static PaymentMethodReportItem From(PaymentsReportMethodSummaryDto dto)
        {
            var applied = FormatCurrency(dto.amountCents);
            var tips = FormatCurrency(dto.tipCents);
            var change = FormatCurrency(dto.changeCents);
            var received = FormatCurrency(dto.receivedAmountCents);
            return new PaymentMethodReportItem
            {
                MethodDisplay = MethodToLabel(dto.method, dto.paymentsCount),
                AmountSummary = $"Aplicado: {applied} · Propinas: {tips}",
                ExtrasSummary = $"Recibido: {received} · Cambio: {change}",
            };
        }

        internal static string MethodToLabel(string code, int count)
        {
            var label = code.ToUpperInvariant() switch
            {
                "CASH" => "Efectivo",
                "CARD" => "Tarjeta",
                "TRANSFER" => "Transferencia",
                "OTHER" => "Otro",
                _ => code
            };
            return $"{label} · {count} pago(s)";
        }

        static string FormatCurrency(int cents) => (cents / 100m).ToString("C", CultureInfo.CurrentCulture);
        }
    }

    public class PaymentOrderReportItem
    {
        public int OrderId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Subtitle { get; init; } = string.Empty;
        public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);
        public string TotalsSummary { get; init; } = string.Empty;
        public string StatusDisplay { get; init; } = string.Empty;
        public Color StatusColor { get; init; } = Colors.Gray;
        public List<PaymentOrderPaymentLine> Payments { get; init; } = new();
        public bool HasPayments => Payments.Count > 0;

        public static PaymentOrderReportItem From(PaymentsReportOrderDto dto)
        {
            var titleParts = new List<string> { dto.code };
            var service = TranslateServiceType(dto.serviceType);
            if (!string.IsNullOrWhiteSpace(service))
                titleParts.Add(service);
            var tableName = dto.table?.name;
            if (!string.IsNullOrWhiteSpace(tableName))
                titleParts.Add(tableName);

            var subtitleParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(dto.note))
                subtitleParts.Add($"Nota: {dto.note}");

            var payments = dto.payments?.Select(PaymentOrderPaymentLine.From).ToList() ?? new List<PaymentOrderPaymentLine>();
            var statusDisplay = BuildStatusSummary(dto);

            return new PaymentOrderReportItem
            {
                OrderId = dto.orderId ?? dto.id,
                Title = string.Join(" • ", titleParts),
                Subtitle = subtitleParts.Count == 0 ? string.Empty : string.Join(" • ", subtitleParts),
                TotalsSummary = $"Subtotal: {PaymentsReportPage.FormatCurrency(dto.subtotalCents)} · Total: {PaymentsReportPage.FormatCurrency(dto.totalCents)}",
                StatusDisplay = statusDisplay,
                StatusColor = GetStatusColor(dto.status),
                Payments = payments
            };
        }

        static string TranslateServiceType(string? serviceType) => serviceType?.ToUpperInvariant() switch
        {
            "DINE_IN" => "En mesa",
            "TAKEAWAY" => "Para llevar",
            "DELIVERY" => "Delivery",
            _ => serviceType ?? string.Empty
        };

        static string BuildStatusSummary(PaymentsReportOrderDto dto)
        {
            var statusLabel = TranslateStatus(dto.status);
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(statusLabel))
                parts.Add(statusLabel);

            if (string.Equals(dto.status, "CLOSED", StringComparison.OrdinalIgnoreCase) && dto.closedAt.HasValue)
                parts.Add($"Cerrado {PaymentsReportPage.ToLocal(dto.closedAt.Value):HH:mm}");
            else if (string.Equals(dto.status, "CANCELED", StringComparison.OrdinalIgnoreCase) && dto.canceledAt.HasValue)
                parts.Add($"Cancelado {PaymentsReportPage.ToLocal(dto.canceledAt.Value):HH:mm}");
            else if (string.Equals(dto.status, "REFUNDED", StringComparison.OrdinalIgnoreCase) && dto.refundedAt.HasValue)
                parts.Add($"Reembolsado {PaymentsReportPage.ToLocal(dto.refundedAt.Value):HH:mm}");

            return parts.Count == 0 ? "Sin estado" : string.Join(" • ", parts);
        }

        static string TranslateStatus(string? status) => status?.ToUpperInvariant() switch
        {
            "CLOSED" => "Cerrado",
            "OPEN" => "Abierto",
            "HOLD" => "En pausa",
            "CANCELED" => "Cancelado",
            "REFUNDED" => "Reembolsado",
            _ => status ?? string.Empty
        };

        static Color GetStatusColor(string? status) => status?.ToUpperInvariant() switch
        {
            "CLOSED" => Color.FromArgb("#33691E"),
            "CANCELED" => Color.FromArgb("#C62828"),
            "REFUNDED" => Color.FromArgb("#6A1B9A"),
            "HOLD" => Color.FromArgb("#F9A825"),
            "OPEN" => Color.FromArgb("#0277BD"),
            _ => Color.FromArgb("#666666")
        };
    }

    public class PaymentOrderPaymentLine
    {
        public string Summary { get; init; } = string.Empty;

        public static PaymentOrderPaymentLine From(PaymentsReportOrderPaymentDto dto)
        {
            var method = PaymentsReportPage.PaymentMethodReportItem.MethodToLabel(dto.method, 0);
            var applied = PaymentsReportPage.FormatCurrency(dto.amountCents);
            var tips = PaymentsReportPage.FormatCurrency(dto.tipCents);
            var change = PaymentsReportPage.FormatCurrency(dto.changeCents);
            var received = PaymentsReportPage.FormatCurrency(dto.receivedAmountCents);
            var time = dto.paidAt?.ToLocalTime().ToString("dd/MM HH:mm");
            var summary = $"{method}: Aplicado {applied}, Propina {tips}, Recibido {received}, Cambio {change}";
            if (!string.IsNullOrWhiteSpace(time))
                summary += $" · {time}";
            return new PaymentOrderPaymentLine { Summary = summary };
        }
    }
