using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using Imdeliceapp;
using Imdeliceapp.Helpers;
using Imdeliceapp.Models;
using Imdeliceapp.Popups;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Networking;
using CommunityToolkit.Maui.Views;

namespace Imdeliceapp.Pages;

[QueryProperty(nameof(OrderId), "orderId")]
public partial class OrderDetailPage : ContentPage
{
    readonly OrdersApi _ordersApi = new();
    readonly MenusApi _menusApi = new();
    readonly ModifiersApi _modifiersApi = new();
    readonly Dictionary<int, List<ModifierGroupDTO>> _modifierGroupsCache = new();
    OrderDetailDTO? _currentOrder;
    readonly TimeSpan _prepEtaTickInterval = TimeSpan.FromSeconds(1);
    IDispatcherTimer? _prepEtaTimer;
    DateTime? _prepEtaDeadline;

    public ObservableCollection<OrderLineVm> ItemLines { get; } = new();
    public ObservableCollection<OrderPaymentVm> PaymentLines { get; } = new();
    public ObservableCollection<TimelineEntryVm> TimelineEntries { get; } = new();

    int _orderId;
    bool _isLoading;

    string _orderCode = "Orden";
    string _statusBadge = string.Empty;
    Color _statusColor = Colors.Gray;
    string _serviceSummary = string.Empty;
    string _tableSummary = string.Empty;
    bool _hasTable;
    string _customerSummary = string.Empty;
    bool _hasCustomer;
    string _noteSummary = string.Empty;
    bool _hasNote;
    string _coversSummary = string.Empty;
    bool _hasCovers;
    string _servedBySummary = string.Empty;
    bool _hasServedBy;
    string _externalRefSummary = string.Empty;
    bool _hasExternalRef;
    string _prepEtaSummary = string.Empty;
    bool _hasPrepEta;
    string _platformMarkupSummary = string.Empty;
    bool _hasPlatformMarkup;
    string _subtotalFormatted = "$0.00";
    string _discountFormatted = "$0.00";
    string _serviceFeeFormatted = "$0.00";
    string _taxFormatted = "$0.00";
    string _totalFormatted = "$0.00";
    string _paymentsTotalsFormatted = "$0.00";
    bool _hasChargesBreakdown;
    string _outstandingSummary = string.Empty;
    bool _hasOutstanding;
    bool _canAddPayment;
    bool _canSplit;
    bool _canRefund;
    bool _isProcessingRefund;
    string _paymentBlockedMessage = string.Empty;
    bool _showPaymentBlockedMessage;

    public OrderDetailPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public int OrderId
    {
        get => _orderId;
        set
        {
            _orderId = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading == value) return;
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string OrderCode
    {
        get => _orderCode;
        set { _orderCode = value; OnPropertyChanged(); }
    }

    public string StatusBadge
    {
        get => _statusBadge;
        set { _statusBadge = value; OnPropertyChanged(); }
    }

    public Color StatusColor
    {
        get => _statusColor;
        set { _statusColor = value; OnPropertyChanged(); }
    }

    public string ServiceSummary
    {
        get => _serviceSummary;
        set { _serviceSummary = value; OnPropertyChanged(); }
    }

    public string TableSummary
    {
        get => _tableSummary;
        set { _tableSummary = value; OnPropertyChanged(); }
    }

    public bool HasTable
    {
        get => _hasTable;
        set { _hasTable = value; OnPropertyChanged(); }
    }

    public string CustomerSummary
    {
        get => _customerSummary;
        set { _customerSummary = value; OnPropertyChanged(); }
    }

    public bool HasCustomer
    {
        get => _hasCustomer;
        set { _hasCustomer = value; OnPropertyChanged(); }
    }

    public string NoteSummary
    {
        get => _noteSummary;
        set { _noteSummary = value; OnPropertyChanged(); }
    }

    public bool HasNote
    {
        get => _hasNote;
        set { _hasNote = value; OnPropertyChanged(); }
    }

    public string CoversSummary
    {
        get => _coversSummary;
        set { _coversSummary = value; OnPropertyChanged(); }
    }

    public bool HasCovers
    {
        get => _hasCovers;
        set { _hasCovers = value; OnPropertyChanged(); }
    }

    public string ServedBySummary
    {
        get => _servedBySummary;
        set { _servedBySummary = value; OnPropertyChanged(); }
    }

    public bool HasServedBy
    {
        get => _hasServedBy;
        set { _hasServedBy = value; OnPropertyChanged(); }
    }

    public string ExternalRefSummary
    {
        get => _externalRefSummary;
        set { _externalRefSummary = value; OnPropertyChanged(); }
    }

    public bool HasExternalRef
    {
        get => _hasExternalRef;
        set { _hasExternalRef = value; OnPropertyChanged(); }
    }

    public string PrepEtaSummary
    {
        get => _prepEtaSummary;
        set { _prepEtaSummary = value; OnPropertyChanged(); }
    }

    public bool HasPrepEta
    {
        get => _hasPrepEta;
        set { _hasPrepEta = value; OnPropertyChanged(); }
    }

    public string PlatformMarkupSummary
    {
        get => _platformMarkupSummary;
        set { _platformMarkupSummary = value; OnPropertyChanged(); }
    }

    public bool HasPlatformMarkup
    {
        get => _hasPlatformMarkup;
        set { _hasPlatformMarkup = value; OnPropertyChanged(); }
    }

    public string SubtotalFormatted
    {
        get => _subtotalFormatted;
        set { _subtotalFormatted = value; OnPropertyChanged(); }
    }

    public string DiscountFormatted
    {
        get => _discountFormatted;
        set { _discountFormatted = value; OnPropertyChanged(); }
    }

    public string ServiceFeeFormatted
    {
        get => _serviceFeeFormatted;
        set { _serviceFeeFormatted = value; OnPropertyChanged(); }
    }

    public bool HasChargesBreakdown
    {
        get => _hasChargesBreakdown;
        set { _hasChargesBreakdown = value; OnPropertyChanged(); }
    }

    public string TaxFormatted
    {
        get => _taxFormatted;
        set { _taxFormatted = value; OnPropertyChanged(); }
    }

    public string TotalFormatted
    {
        get => _totalFormatted;
        set { _totalFormatted = value; OnPropertyChanged(); }
    }

    public string PaymentsTotalsFormatted
    {
        get => _paymentsTotalsFormatted;
        set { _paymentsTotalsFormatted = value; OnPropertyChanged(); }
    }

    public string OutstandingSummary
    {
        get => _outstandingSummary;
        set { _outstandingSummary = value; OnPropertyChanged(); }
    }

    public bool HasOutstanding
    {
        get => _hasOutstanding;
        set { _hasOutstanding = value; OnPropertyChanged(); }
    }

    public bool CanAddPayment
    {
        get => _canAddPayment;
        set
        {
            if (_canAddPayment == value) return;
            _canAddPayment = value;
            OnPropertyChanged();
        }
    }

    public string PaymentBlockedMessage
    {
        get => _paymentBlockedMessage;
        set
        {
            if (_paymentBlockedMessage == value) return;
            _paymentBlockedMessage = value;
            OnPropertyChanged();
        }
    }

    public bool ShowPaymentBlockedMessage
    {
        get => _showPaymentBlockedMessage;
        set
        {
            if (_showPaymentBlockedMessage == value) return;
            _showPaymentBlockedMessage = value;
            OnPropertyChanged();
        }
    }

    public bool HasTimeline => TimelineEntries.Any();

    public bool CanUpdate => Perms.OrdersUpdate;
    public bool CanSplit
    {
        get => _canSplit;
        set
        {
            if (_canSplit == value) return;
            _canSplit = value;
            OnPropertyChanged();
        }
    }

    public bool CanRefund
    {
        get => _canRefund;
        set
        {
            if (_canRefund == value) return;
            _canRefund = value;
            OnPropertyChanged();
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        OnPropertyChanged(nameof(CanUpdate));
        OnPropertyChanged(nameof(CanSplit));
        OnPropertyChanged(nameof(CanAddPayment));
        OnPropertyChanged(nameof(CanRefund));
        await LoadOrderAsync();
    }

    protected override void OnDisappearing()
    {
        StopPrepEtaTimer();
        base.OnDisappearing();
    }

    async Task LoadOrderAsync()
    {
        if (OrderId <= 0)
            return;

        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await ErrorHandler.MostrarErrorUsuario("Sin conexión a Internet.");
                return;
            }

            IsLoading = true;
            var dto = await _ordersApi.GetAsync(OrderId);
            if (dto == null)
            {
                await ErrorHandler.MostrarErrorUsuario("No se encontró la orden seleccionada.");
                return;
            }

            RenderOrder(dto);
        }
        catch (HttpRequestException ex)
        {
            var message = ErrorHandler.ObtenerMensajeHttp(new HttpResponseMessage(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), ex.Message);
            await ErrorHandler.MostrarErrorUsuario(message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Órdenes – Detalle");
        }
        finally
        {
            IsLoading = false;
        }
    }

    void RenderOrder(OrderDetailDTO dto)
    {
        _currentOrder = dto;
        OrderCode = dto.code;
        StatusBadge = dto.status switch
        {
            "OPEN" => "Abierto",
            "HOLD" => "En pausa",
            "CLOSED" => "Cerrado",
            "CANCELED" => "Cancelado",
            "DRAFT" => "Borrador",
            _ => dto.status
        };

        StatusColor = dto.status switch
        {
            "OPEN" => Color.FromArgb("#00796B"),
            "HOLD" => Color.FromArgb("#FFA000"),
            "CLOSED" => Color.FromArgb("#33691E"),
            "CANCELED" => Color.FromArgb("#D32F2F"),
            "DRAFT" => Color.FromArgb("#546E7A"),
            _ => Colors.Gray
        };
        var normalizedStatus = dto.status?.ToUpperInvariant() ?? string.Empty;

        var serviceLabel = dto.serviceType switch
        {
            "DINE_IN" => "Servicio en mesa",
            "TAKEAWAY" => "Para llevar",
            "DELIVERY" => "Entrega a domicilio",
            _ => dto.serviceType
        };
        var parts = new List<string> { serviceLabel };
        if (!string.IsNullOrWhiteSpace(dto.source))
            parts.Add(dto.source);
        if (dto.openedAt.HasValue)
            parts.Add($"Abierta {dto.openedAt.Value.ToLocalTime():dd/MM HH:mm}");
        ServiceSummary = string.Join(" • ", parts);

        if (dto.table != null)
        {
            TableSummary = $"{dto.table.name} · {dto.table.seats ?? 0} lugares";
            HasTable = true;
        }
        else if (dto.tableId.HasValue)
        {
            TableSummary = $"Mesa {dto.tableId.Value}";
            HasTable = true;
        }
        else
        {
            TableSummary = string.Empty;
            HasTable = false;
        }

        if (!string.IsNullOrWhiteSpace(dto.customerName) || !string.IsNullOrWhiteSpace(dto.customerPhone))
        {
            CustomerSummary = $"{dto.customerName} {dto.customerPhone}".Trim();
            HasCustomer = true;
        }
        else
        {
            CustomerSummary = string.Empty;
            HasCustomer = false;
        }

        if (!string.IsNullOrWhiteSpace(dto.note))
        {
            NoteSummary = $"Nota: {dto.note}";
            HasNote = true;
        }
        else
        {
            NoteSummary = string.Empty;
            HasNote = false;
        }

        if (dto.covers.HasValue && dto.covers.Value > 0)
        {
            CoversSummary = $"{dto.covers.Value} comensal{(dto.covers.Value == 1 ? string.Empty : "es")}";
            HasCovers = true;
        }
        else
        {
            CoversSummary = string.Empty;
            HasCovers = false;
        }

        if (dto.servedBy != null)
        {
            var servedByParts = new List<string> { dto.servedBy.name ?? $"Usuario {dto.servedBy.id}" };
            if (!string.IsNullOrWhiteSpace(dto.servedBy.email))
                servedByParts.Add(dto.servedBy.email);
            ServedBySummary = $"Atiende: {string.Join(" · ", servedByParts)}";
            HasServedBy = true;
        }
        else if (dto.servedByUserId.HasValue)
        {
            ServedBySummary = $"Atiende: usuario #{dto.servedByUserId.Value}";
            HasServedBy = true;
        }
        else
        {
            ServedBySummary = string.Empty;
            HasServedBy = false;
        }

        if (!string.IsNullOrWhiteSpace(dto.externalRef))
        {
            ExternalRefSummary = $"Referencia externa: {dto.externalRef}";
            HasExternalRef = true;
        }
        else
        {
            ExternalRefSummary = string.Empty;
            HasExternalRef = false;
        }

        ConfigurePrepEta(dto);

        if (dto.platformMarkupPct.HasValue)
        {
            var channelLabel = string.IsNullOrWhiteSpace(dto.source) ? string.Empty : $" ({dto.source})";
            PlatformMarkupSummary = $"Markup plataforma{channelLabel}: {dto.platformMarkupPct.Value:0.##}%";
            HasPlatformMarkup = true;
        }
        else
        {
            PlatformMarkupSummary = string.Empty;
            HasPlatformMarkup = false;
        }

        SubtotalFormatted = FormatCurrency(dto.subtotalCents);
        DiscountFormatted = FormatCurrency(dto.discountCents);
        ServiceFeeFormatted = FormatCurrency(dto.serviceFeeCents);
        TaxFormatted = FormatCurrency(dto.taxCents);
        HasChargesBreakdown =
            dto.discountCents != 0 ||
            dto.serviceFeeCents != 0 ||
            dto.taxCents != 0 ||
            dto.subtotalCents != dto.totalCents;
        TotalFormatted = FormatCurrency(dto.totalCents);

        var paidBase = dto.paymentsTotalCents;
        var paidTips = dto.paymentsTipCents;
        var paid = paidBase + paidTips;
        if (paid > 0)
        {
            var paidMessage = $"Pagado {FormatCurrency(paidBase)}";
            if (paidTips > 0)
                paidMessage += $" · Propinas {FormatCurrency(paidTips)}";
            PaymentsTotalsFormatted = paidMessage;
        }
        else
        {
            PaymentsTotalsFormatted = "Sin pagos";
        }

        var hasPayments = (dto.payments?.Count ?? 0) > 0 || dto.paymentsTotalCents != 0 || dto.paymentsTipCents != 0;
        var canRefundStatus = string.Equals(dto.status, "CLOSED", StringComparison.OrdinalIgnoreCase);
        var hasRefundPerms = Perms.OrdersUpdate && Perms.OrdersRefund;
        CanRefund = hasRefundPerms && canRefundStatus && hasPayments;
        if (string.Equals(dto.status, "REFUNDED", StringComparison.OrdinalIgnoreCase))
            CanRefund = false;

        var outstanding = dto.totalCents - paid;
        var isClosed = string.Equals(dto.status, "CLOSED", StringComparison.OrdinalIgnoreCase);
        var itemsReady = AreAllItemsReady(dto.items);
        if (outstanding > 0 && !isClosed)
        {
            OutstandingSummary = $"Faltan {FormatCurrency(outstanding)} por cobrar";
            HasOutstanding = true;
        }
        else if (outstanding < 0)
        {
            OutstandingSummary = $"Saldo a favor {FormatCurrency(Math.Abs(outstanding))}";
            HasOutstanding = true;
        }
        else
        {
            OutstandingSummary = string.Empty;
            HasOutstanding = false;
        }

        CanAddPayment = Perms.OrdersUpdate && outstanding > 0 && !isClosed && itemsReady;
        if (!itemsReady && outstanding > 0 && !isClosed)
        {
            PaymentBlockedMessage = "Para registrar un pago todos los productos deben estar Listos o Servidos.";
            ShowPaymentBlockedMessage = true;
        }
        else
        {
            PaymentBlockedMessage = string.Empty;
            ShowPaymentBlockedMessage = false;
        }

        ItemLines.Clear();
        var parents = dto.items?.Where(i => i.parentItemId == null).ToList() ?? new();
        foreach (var item in parents)
            AppendItemRecursive(item, 0);

        TimelineEntries.Clear();
        foreach (var entry in BuildTimeline(dto))
            TimelineEntries.Add(entry);
        OnPropertyChanged(nameof(HasTimeline));

        PaymentLines.Clear();
        foreach (var payment in dto.payments ?? new List<OrderPaymentDTO>())
            PaymentLines.Add(OrderPaymentVm.From(payment));

        UpdateSplitAvailability(dto);
    }

    void ConfigurePrepEta(OrderDetailDTO dto)
    {
        StopPrepEtaTimer();

        if (!dto.prepEtaMinutes.HasValue || dto.prepEtaMinutes.Value <= 0)
        {
            _prepEtaDeadline = null;
            PrepEtaSummary = string.Empty;
            HasPrepEta = false;
            return;
        }

        var reference = dto.acceptedAt ?? dto.openedAt;
        if (!reference.HasValue)
        {
            _prepEtaDeadline = null;
            PrepEtaSummary = $"ETA: {dto.prepEtaMinutes.Value} min";
            HasPrepEta = true;
            return;
        }

        var start = NormalizeToLocal(reference.Value);
        _prepEtaDeadline = start + TimeSpan.FromMinutes(dto.prepEtaMinutes.Value);
        HasPrepEta = true;
        UpdatePrepEtaSummary();
        StartPrepEtaTimer();
    }

    void StartPrepEtaTimer()
    {
        if (_prepEtaDeadline == null || Dispatcher == null)
            return;

        _prepEtaTimer = Dispatcher.CreateTimer();
        _prepEtaTimer.Interval = _prepEtaTickInterval;
        _prepEtaTimer.Tick += PrepEtaTimer_Tick;
        _prepEtaTimer.Start();
    }

    void StopPrepEtaTimer()
    {
        if (_prepEtaTimer == null) return;
        _prepEtaTimer.Tick -= PrepEtaTimer_Tick;
        _prepEtaTimer.Stop();
        _prepEtaTimer = null;
    }

    void PrepEtaTimer_Tick(object? sender, EventArgs e) => UpdatePrepEtaSummary();

    void UpdatePrepEtaSummary()
    {
        if (!_prepEtaDeadline.HasValue)
        {
            PrepEtaSummary = string.Empty;
            HasPrepEta = false;
            return;
        }

        var remaining = _prepEtaDeadline.Value - DateTime.Now;
        if (remaining <= TimeSpan.Zero)
        {
            PrepEtaSummary = "Tiempo restante: 00:00";
            HasPrepEta = true;
            StopPrepEtaTimer();
        }
        else
        {
            PrepEtaSummary = $"Tiempo restante: {FormatEtaRemaining(remaining)}";
            HasPrepEta = true;
        }
    }

    static DateTime NormalizeToLocal(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value.ToLocalTime(),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Local),
        _ => value
    };

    static string FormatEtaRemaining(TimeSpan remaining)
    {
        if (remaining.TotalHours >= 1)
            return $"{(int)remaining.TotalHours}h {remaining.Minutes:D2}m";
        return $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
    }

    void AppendItemRecursive(OrderItemDTO item, int level)
    {
        ItemLines.Add(OrderLineVm.From(item, level));
        if (item.childItems == null) return;

        foreach (var child in item.childItems)
            AppendItemRecursive(child, level + 1);
    }

    IEnumerable<TimelineEntryVm> BuildTimeline(OrderDetailDTO dto)
    {
        var timeline = new List<TimelineEntryVm>();

        if (dto.openedAt.HasValue)
            timeline.Add(TimelineEntryVm.Create("Creado", dto.openedAt));
        if (dto.acceptedAt.HasValue)
            timeline.Add(TimelineEntryVm.Create("Aceptado", dto.acceptedAt));
        if (dto.readyAt.HasValue)
            timeline.Add(TimelineEntryVm.Create("Listo", dto.readyAt));
        if (dto.servedAt.HasValue)
            timeline.Add(TimelineEntryVm.Create("Servido", dto.servedAt));
        if (dto.closedAt.HasValue)
            timeline.Add(TimelineEntryVm.Create("Cerrado", dto.closedAt));
        if (dto.canceledAt.HasValue)
            timeline.Add(TimelineEntryVm.Create("Cancelado", dto.canceledAt));

        return timeline;
    }

    static List<string> BuildAllowedStatuses(string? currentStatus)
    {
        var allowed = new List<string>();
        var status = currentStatus?.ToUpperInvariant() ?? "OPEN";

        switch (status)
        {
            case "DRAFT":
                allowed.AddRange(new[] { "OPEN", "HOLD", "CANCELED" });
                break;
            case "OPEN":
                allowed.AddRange(new[] { "HOLD", "CANCELED", "CLOSED" });
                break;
            case "HOLD":
                allowed.AddRange(new[] { "OPEN", "CANCELED" });
                break;
            case "CLOSED":
            case "CANCELED":
                break;
            default:
                allowed.AddRange(new[] { "OPEN", "HOLD", "CANCELED" });
                break;
        }

        return allowed.Distinct().ToList();
    }

    static string FormatCurrency(int cents)
    {
        var value = cents / 100m;
        return value.ToString("C", CultureInfo.CurrentCulture);
    }

    async void AddPayment_Clicked(object sender, EventArgs e)
    {
        if (!Perms.OrdersUpdate)
        {
            await ErrorHandler.MostrarErrorUsuario("No tienes permisos para registrar pagos.");
            return;
        }

        if (!CanAddPayment)
        {
            var message = ShowPaymentBlockedMessage && !string.IsNullOrWhiteSpace(PaymentBlockedMessage)
                ? PaymentBlockedMessage
                : "El pedido ya está pagado.";
            await ErrorHandler.MostrarErrorUsuario(message);
            return;
        }

        if (_currentOrder == null)
        {
            await LoadOrderAsync();
            if (_currentOrder == null)
            {
                await ErrorHandler.MostrarErrorUsuario("No se pudo cargar la orden actual.");
                return;
            }
        }

        var outstandingCents = (_currentOrder?.totalCents ?? 0)
                               - ((_currentOrder?.paymentsTotalCents ?? 0) + (_currentOrder?.paymentsTipCents ?? 0));
        var popup = new AddPaymentPopup(outstandingCents);
        if (await this.ShowPopupAsync(popup) is not AddPaymentPopup.Result result || result.Payload == null)
            return;

        await SubmitPaymentAsync(result.Payload);
    }

    async void ChangeStatus_Clicked(object sender, EventArgs e)
    {
        if (!Perms.OrdersUpdate)
        {
            await ErrorHandler.MostrarErrorUsuario("No tienes permisos para cambiar el estado.");
            return;
        }

        if (_currentOrder == null)
            return;

        var nextStatuses = BuildAllowedStatuses(_currentOrder.status);
        if (nextStatuses.Count == 0)
        {
            await ErrorHandler.MostrarErrorUsuario("Este pedido ya no puede cambiar de estado.");
            return;
        }

        var popup = new OrderStatusPopup(
            _currentOrder.status,
            nextStatuses.Select(code => new OrderStatusPopup.StatusOption(code, OrderStatusPopup.StatusOption.GetDisplayName(code))).ToList());

        if (await this.ShowPopupAsync(popup) is not UpdateOrderStatusDto statusDto)
            return;
        var requestSnapshot = statusDto;

        if (string.Equals(statusDto.status, "CLOSED", StringComparison.OrdinalIgnoreCase))
        {
            var totalPaid = _currentOrder.payments?.Sum(p => p.amountCents + p.tipCents) ?? 0;
            if (totalPaid < _currentOrder.totalCents)
            {
                var pending = _currentOrder.totalCents - totalPaid;
                await ErrorHandler.MostrarErrorUsuario(
                    $"No puedes cerrar la orden hasta registrar el pago completo. Pendiente por cobrar: {FormatCurrency(pending)}.");
                return;
            }
        }

        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await ErrorHandler.MostrarErrorUsuario("Sin conexión a Internet.");
                return;
            }

            IsLoading = true;
            var updated = await _ordersApi.UpdateStatusAsync(OrderId, statusDto);
            if (updated != null)
                RenderOrder(updated);
            else
                await LoadOrderAsync();
        }
        catch (HttpRequestException ex)
        {
            var message = ErrorHandler.ObtenerMensajeHttp(new HttpResponseMessage(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), ex.Message);
#if DEBUG
        var payload = JsonSerializer.Serialize(requestSnapshot, new JsonSerializerOptions { WriteIndented = true });
        var debugInfo = $"Payload enviado:\n{payload}\n\nRespuesta del servidor:\n{ex.Message}";
        await Clipboard.Default.SetTextAsync(debugInfo);
        await ErrorHandler.MostrarErrorTecnico(new Exception("Detalle copiado al portapapeles."), "Órdenes – Cambiar estado (debug)");
#endif
            await ErrorHandler.MostrarErrorUsuario(message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Órdenes – Cambiar estado");
        }
        finally
        {
            IsLoading = false;
        }
    }

    async Task SubmitPaymentAsync(AddPaymentDto dto)
    {
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await ErrorHandler.MostrarErrorUsuario("Sin conexión a Internet.");
                return;
            }

            IsLoading = true;
            var payment = await _ordersApi.AddPaymentAsync(OrderId, dto);
            if (payment != null)
                await LoadOrderAsync();
            else
                await LoadOrderAsync();
        }
        catch (HttpRequestException ex)
        {
            var message = ErrorHandler.ObtenerMensajeHttp(new HttpResponseMessage(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), ex.Message);
#if DEBUG
            var payload = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            var debugInfo = $"Payload enviado:\n{payload}\n\nRespuesta del servidor:\n{ex.Message}";
            await Clipboard.Default.SetTextAsync(debugInfo);
            await ErrorHandler.MostrarErrorTecnico(new Exception("Detalle copiado al portapapeles."), "Órdenes – Agregar pago (debug)");
#endif
            await ErrorHandler.MostrarErrorUsuario(message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Órdenes – Agregar pago");
        }
        finally
        {
            IsLoading = false;
        }
    }

    async void Back_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    async void OpenEditItemPicker_Clicked(object sender, EventArgs e)
    {
        if (!Perms.OrdersUpdate)
        {
            await ErrorHandler.MostrarErrorUsuario("No tienes permisos para modificar los productos.");
            return;
        }

        if (_currentOrder == null)
        {
            await LoadOrderAsync();
            if (_currentOrder == null)
                return;
        }

        while (true)
        {
#if DEBUG
            var debugInfo = BuildSelectionDebugReport("editar");
            await Clipboard.Default.SetTextAsync(debugInfo);
            await ErrorHandler.MostrarErrorTecnico(new Exception("Lista copiada al portapapeles."), "Modo debug – editar productos");
#endif

            var result = await ShowItemsManagerPopupAsync();
            if (result == null)
                break;

            switch (result.Action)
            {
                case OrderItemsManagerPopup.ItemAction.AddProduct:
                    await HandleAddProductAsync();
                    break;
                case OrderItemsManagerPopup.ItemAction.Edit when result.Item != null:
                    await HandleEditItemAsync(result.Item);
                    break;
                case OrderItemsManagerPopup.ItemAction.ChangeStatus when result.Item != null:
                    await HandleChangeItemStatusAsync(result.Item);
                    break;
                case OrderItemsManagerPopup.ItemAction.Delete when result.Item != null:
                    await HandleDeleteItemAsync(result.Item);
                    break;
            }
        }
    }

    async Task<OrderItemsManagerPopup.Result?> ShowItemsManagerPopupAsync()
    {
        if (_currentOrder == null)
            return null;

        var rootItems = _currentOrder.items?
            .Where(i => i.parentItemId == null)
            .ToList() ?? new List<OrderItemDTO>();

        var popup = new OrderItemsManagerPopup(rootItems);
        var result = await this.ShowPopupAsync(popup);
        return result as OrderItemsManagerPopup.Result;
    }
    async void SplitOrder_Clicked(object sender, EventArgs e)
    {
        if (!Perms.OrdersUpdate)
        {
            await ErrorHandler.MostrarErrorUsuario("No tienes permisos para dividir pedidos.");
            return;
        }

        if (_currentOrder?.items == null || _currentOrder.items.Count == 0)
        {
            await ErrorHandler.MostrarErrorUsuario("No hay productos disponibles para dividir.");
            return;
        }

#if DEBUG
        var debugInfo = BuildSelectionDebugReport("dividir");
        await Clipboard.Default.SetTextAsync(debugInfo);
        await ErrorHandler.MostrarErrorTecnico(new Exception("Lista copiada al portapapeles."), "Modo debug – dividir pedido");
#endif

        var rootItems = _currentOrder.items.Where(i => i.parentItemId == null).ToList();
        if (rootItems.Count == 0)
        {
            await ErrorHandler.MostrarErrorUsuario("No hay productos principales disponibles.");
            return;
        }

        var currentTableId = _currentOrder.table?.id ?? _currentOrder.tableId;
        var currentTableName = _currentOrder.table?.name ?? (currentTableId.HasValue ? $"Mesa {currentTableId.Value}" : null);
        var popup = new OrderSplitPopup(rootItems, _currentOrder.serviceType, currentTableId, _currentOrder.covers, currentTableName);

        if (await this.ShowPopupAsync(popup) is not OrderSplitPopup.SplitPlanResult splitPlan)
            return;

        await SubmitSplitAsync(splitPlan);
    }

    async void RefundOrder_Clicked(object sender, EventArgs e)
    {
        await RefundCurrentOrderAsync();
    }

    async Task HandleAddProductAsync()
    {
        while (true)
        {
            var selector = new OrderMenuSelectorPopup();
            if (await this.ShowPopupAsync(selector) is not OrderMenuSelectorPopup.SelectionResult selection)
                return;

            var menuItem = selection.MenuItem;
            if (string.Equals(menuItem.Kind, "COMBO", StringComparison.OrdinalIgnoreCase))
            {
                await ErrorHandler.MostrarErrorUsuario("Los combos solo se pueden armar desde la pantalla de toma de pedidos.");
                continue;
            }

            if (!menuItem.ProductId.HasValue)
            {
                await ErrorHandler.MostrarErrorUsuario("El producto seleccionado no es válido.");
                continue;
            }

            var modifierGroups = await GetModifierGroupsForProductAsync(menuItem.ProductId.Value);
            var configureResult = new ConfigureMenuItemResult(
                menuItem,
                menuItem,
                1,
                null,
                new List<TakeOrderPage.CartModifierSelection>(),
                Array.Empty<TakeOrderPage.ComboChildSelection>(),
                Guid.NewGuid());
            var cartEntry = new TakeOrderPage.CartEntry(configureResult);

            var popup = new ConfigureMenuItemPopup(
                menuItem,
                selection.Variants,
                modifierGroups,
                cartEntry,
                variantRulesLoader: async variantId =>
                {
                    var links = await _menusApi.GetVariantModifierGroupsAsync(variantId);
                    return links ?? new List<VariantModifierGroupLinkDTO>();
                },
                lockQuantity: false,
                lockedQuantity: null,
                confirmButtonText: "Agregar producto",
                comboChildConfigurations: null,
                showBackButton: true);

            var configResult = await this.ShowPopupAsync(popup);
            if (popup.BackRequested)
                continue;

            if (configResult is not ConfigureMenuItemResult finalResult)
                return;

            var dto = BuildAddOrderItemDto(finalResult);
            if (dto == null)
            {
                await ErrorHandler.MostrarErrorUsuario("No se pudo construir el producto a agregar.");
                continue;
            }

            await SubmitAddItemAsync(dto);
            return;
        }
    }

    async Task HandleEditItemAsync(OrderItemDTO item)
    {
        var lockQuantity = ShouldLockQuantity(item);
        if (await TryOpenAdvancedEditorAsync(item, lockQuantity))
            return;

        var modifierOptions = await BuildModifierOptionsAsync(item);
        var popup = new OrderItemEditorPopup(item, modifierOptions, lockQuantity);
        if (await this.ShowPopupAsync(popup) is not OrderItemEditorPopup.Result result || result.Payload == null)
            return;

        await SubmitItemUpdateAsync(item.id, result.Payload, "Órdenes – Editar ítem");
    }

    async Task<bool> TryOpenAdvancedEditorAsync(OrderItemDTO item, bool lockQuantity)
    {
        var context = await BuildAdvancedEditorContextAsync(item);
        if (context == null)
            return false;

        var popup = new ConfigureMenuItemPopup(
            context.BaseItem,
            context.Variants,
            context.ModifierGroups,
            context.ExistingEntry,
            context.VariantRulesLoader,
            lockQuantity: lockQuantity,
            lockedQuantity: lockQuantity ? item.quantity : null,
            confirmButtonText: "Guardar cambios",
            comboChildConfigurations: null);

        if (await this.ShowPopupAsync(popup) is not ConfigureMenuItemResult configureResult)
            return true;

        var dto = new UpdateOrderItemDto
        {
            quantity = lockQuantity ? null : configureResult.Quantity,
            notes = string.IsNullOrWhiteSpace(configureResult.Notes) ? null : configureResult.Notes.Trim(),
            replaceModifiers = BuildModifierPayload(configureResult)
        };

        await SubmitItemUpdateAsync(item.id, dto, "Órdenes – Editar ítem avanzado");
        return true;
    }

    async Task HandleChangeItemStatusAsync(OrderItemDTO item)
    {
        var popup = new OrderItemStatusPopup(item.status);
        if (await this.ShowPopupAsync(popup) is not UpdateOrderItemStatusDto statusDto)
            return;

        if (!CanUpdateComboStatus(item, statusDto.status, out var warning))
        {
            await DisplayAlert("Combo incompleto", warning, "Entendido");
            return;
        }

        var requestSnapshot = statusDto;

        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await ErrorHandler.MostrarErrorUsuario("Sin conexión a Internet.");
                return;
            }

            IsLoading = true;
            var updated = await _ordersApi.UpdateItemStatusAsync(item.id, statusDto);
            if (updated != null)
                await LoadOrderAsync();
            else
                await LoadOrderAsync();
        }
        catch (HttpRequestException ex)
        {
            var message = ErrorHandler.ObtenerMensajeHttp(new HttpResponseMessage(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), ex.Message);
#if DEBUG
            var payload = JsonSerializer.Serialize(requestSnapshot, new JsonSerializerOptions { WriteIndented = true });
            var debugInfo = $"Payload enviado:\n{payload}\n\nRespuesta del servidor:\n{ex.Message}";
            await Clipboard.Default.SetTextAsync(debugInfo);
            await ErrorHandler.MostrarErrorTecnico(new Exception("Detalle copiado al portapapeles."), "Órdenes – Estado de ítem (debug)");
#endif
            await ErrorHandler.MostrarErrorUsuario(message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Órdenes – Cambiar estado de ítem");
        }
        finally
        {
            IsLoading = false;
        }
    }

    static bool CanUpdateComboStatus(OrderItemDTO item, string targetStatus, out string? message)
    {
        message = null;
        var requiredRank = GetRequiredChildRank(targetStatus);
        if (requiredRank == null || item.childItems == null || item.childItems.Count == 0)
            return true;

        var blocking = item.childItems
            .Where(child => GetStatusRank(child.status) < requiredRank.Value)
            .ToList();

        if (blocking.Count == 0)
            return true;

        message = requiredRank.Value == GetStatusRank("READY")
            ? "Este combo aún tiene componentes pendientes. Marca cada componente como \"Listo\" antes de completar el combo."
            : "Este combo aún tiene componentes sin servir. Marca cada componente como \"Servido\" antes de cerrar el combo.";
        return false;
    }

    static int? GetRequiredChildRank(string targetStatus)
    {
        switch ((targetStatus ?? string.Empty).ToUpperInvariant())
        {
            case "READY":
                return GetStatusRank("READY");
            case "SERVED":
                return GetStatusRank("SERVED");
            default:
                return null;
        }
    }

    static int GetStatusRank(string? status)
    {
        return status?.ToUpperInvariant() switch
        {
            "NEW" => 0,
            "IN_PROGRESS" => 1,
            "READY" => 2,
            "SERVED" => 3,
            "CANCELED" => 4,
            _ => -1
        };
    }

    async Task HandleDeleteItemAsync(OrderItemDTO item)
    {
        if (item.parentItemId.HasValue)
        {
            var combo = _currentOrder?.items?.FirstOrDefault(i => i.id == item.parentItemId.Value);
            if (combo != null)
            {
                var mensaje = $"“{item.nameSnapshot ?? item.product?.name ?? "Producto"}” pertenece al combo \"{combo.nameSnapshot ?? combo.product?.name}\". Debes eliminar el combo completo.";
                var eliminarCombo = await DisplayAlert("Eliminar combo", $"{mensaje}\n\n¿Quieres eliminar todo el combo?", "Eliminar combo", "Cancelar");
                if (!eliminarCombo)
                    return;
                item = combo;
            }
        }
        else if (item.childItems?.Count > 0)
        {
            var continuar = await DisplayAlert("Eliminar combo", "Este producto es un combo. Al eliminarlo se quitarán todos sus componentes. ¿Deseas continuar?", "Eliminar combo", "Cancelar");
            if (!continuar)
                return;
        }

        var confirmar = await DisplayAlert(
            "Eliminar producto",
            $"¿Seguro que quieres quitar \"{item.nameSnapshot ?? item.product?.name}\" del pedido?",
            "Sí, eliminar",
            "Cancelar");

        if (!confirmar)
            return;

        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await ErrorHandler.MostrarErrorUsuario("Sin conexión a Internet.");
                return;
            }

            IsLoading = true;
            await _ordersApi.DeleteItemAsync(item.id);
            await LoadOrderAsync();
        }
        catch (HttpRequestException ex)
        {
            var message = ErrorHandler.ObtenerMensajeHttp(new HttpResponseMessage(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), ex.Message);
#if DEBUG
            var debugInfo = $"Delete /api/orders/items/{item.id}\nRespuesta del servidor:\n{ex.Message}";
            await Clipboard.Default.SetTextAsync(debugInfo);
            await ErrorHandler.MostrarErrorTecnico(new Exception("Detalle copiado al portapapeles."), "Órdenes – Eliminar ítem (debug)");
#endif
            await ErrorHandler.MostrarErrorUsuario(message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Órdenes – Eliminar ítem");
        }
        finally
        {
            IsLoading = false;
        }
    }

    async void EditMeta_Clicked(object sender, EventArgs e)
    {
        if (!CanUpdate)
            return;

        if (_currentOrder == null)
            return;

        var isDineIn = string.Equals(_currentOrder.serviceType, "DINE_IN", StringComparison.OrdinalIgnoreCase);
        var popup = new OrderMetaEditorPopup(
            _currentOrder.table?.id ?? _currentOrder.tableId,
            _currentOrder.covers,
            _currentOrder.note,
            _currentOrder.prepEtaMinutes,
            isDineIn);

        if (await this.ShowPopupAsync(popup) is not UpdateOrderMetaDto dto)
            return;

        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await ErrorHandler.MostrarErrorUsuario("Sin conexión a Internet.");
                return;
            }

            IsLoading = true;
            var updated = await _ordersApi.UpdateMetaAsync(OrderId, dto);
            if (!updated)
            {
                await ErrorHandler.MostrarErrorUsuario("No se pudo actualizar la información del pedido.");
                return;
            }
        }
        catch (HttpRequestException ex)
        {
            var message = ErrorHandler.ObtenerMensajeHttp(new HttpResponseMessage(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), ex.Message);
            await ErrorHandler.MostrarErrorUsuario(message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Órdenes – Actualizar encabezado");
        }
        finally
        {
            IsLoading = false;
        }

        await LoadOrderAsync();
    }

    static bool IsComboParent(OrderItemDTO item) => item.childItems != null && item.childItems.Count > 0;
    static bool IsComboChild(OrderItemDTO item) => item.parentItemId.HasValue;
    static bool ShouldLockQuantity(OrderItemDTO item) => IsComboChild(item);

    async Task<IReadOnlyList<OrderItemEditorPopup.ModifierOptionChoice>> BuildModifierOptionsAsync(OrderItemDTO item)
    {
        try
        {
            if (item.variantId.HasValue)
            {
                var links = await _menusApi.GetVariantModifierGroupsAsync(item.variantId.Value) ?? new List<VariantModifierGroupLinkDTO>();
                var flattened = new List<OrderItemEditorPopup.ModifierOptionChoice>();
                foreach (var link in links)
                {
                    var groupName = link.group?.name ?? $"Grupo #{link.groupId}";
                    foreach (var option in link.group?.options ?? new List<ModifierOptionDTO>())
                    {
                        var label = $"{groupName} · {option.name}";
                        if (option.priceExtraCents > 0)
                            label += $" (+{FormatCurrency(option.priceExtraCents)})";
                        flattened.Add(new OrderItemEditorPopup.ModifierOptionChoice(option.id, label));
                    }
                }
                if (flattened.Count > 0)
                    return flattened;
            }
        }
        catch
        {
            // fallback to existing modifiers
        }

        var fallback = item.modifiers?
            .Select(m => new OrderItemEditorPopup.ModifierOptionChoice(
                m.optionId,
                m.nameSnapshot ?? $"Opción {m.optionId}"))
            .GroupBy(m => m.OptionId)
            .Select(g => g.First())
            .ToList() ?? new List<OrderItemEditorPopup.ModifierOptionChoice>();

        return fallback;
    }

    async Task SubmitItemUpdateAsync(int itemId, UpdateOrderItemDto payload, string debugContext)
    {
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await ErrorHandler.MostrarErrorUsuario("Sin conexión a Internet.");
                return;
            }

            IsLoading = true;
            var updated = await _ordersApi.UpdateItemAsync(itemId, payload);
            if (updated != null)
                await LoadOrderAsync();
            else
                await LoadOrderAsync();
        }
        catch (HttpRequestException ex)
        {
            var message = ErrorHandler.ObtenerMensajeHttp(new HttpResponseMessage(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), ex.Message);
#if DEBUG
            var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            var debugInfo = $"Payload enviado:\n{payloadJson}\n\nRespuesta del servidor:\n{ex.Message}";
            await Clipboard.Default.SetTextAsync(debugInfo);
            await ErrorHandler.MostrarErrorTecnico(new Exception("Detalle copiado al portapapeles."), $"{debugContext} (debug)");
#endif
            await ErrorHandler.MostrarErrorUsuario(message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, debugContext);
        }
        finally
        {
            IsLoading = false;
        }
    }

    async Task SubmitAddItemAsync(AddOrderItemDto payload)
    {
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await ErrorHandler.MostrarErrorUsuario("Sin conexión a Internet.");
                return;
            }

            IsLoading = true;
            await _ordersApi.AddItemAsync(OrderId, payload);
            await LoadOrderAsync();
        }
        catch (HttpRequestException ex)
        {
            var message = ErrorHandler.ObtenerMensajeHttp(new HttpResponseMessage(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), ex.Message);
#if DEBUG
            var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            var debugInfo = $"Payload enviado:\n{payloadJson}\n\nRespuesta del servidor:\n{ex.Message}";
            await Clipboard.Default.SetTextAsync(debugInfo);
            await ErrorHandler.MostrarErrorTecnico(new Exception("Detalle copiado al portapapeles."), "Órdenes – Agregar producto (debug)");
#endif
            await ErrorHandler.MostrarErrorUsuario(message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Órdenes – Agregar producto");
        }
        finally
        {
            IsLoading = false;
        }
    }

    void UpdateSplitAvailability(OrderDetailDTO dto)
    {
        if (!Perms.OrdersUpdate)
        {
            CanSplit = false;
            return;
        }

        var roots = dto.items?
            .Where(i => i.parentItemId == null)
            .ToList() ?? new List<OrderItemDTO>();

        if (roots.Count == 0)
        {
            CanSplit = false;
            return;
        }

        if (roots.Count > 1)
        {
            CanSplit = true;
            return;
        }

        var single = roots[0];
        var canSplitSingle = single.quantity > 1 && !IsComboParent(single);
        CanSplit = canSplitSingle;
    }

    async Task SubmitSplitAsync(OrderSplitPopup.SplitPlanResult splitPlan)
    {
        if (splitPlan.Items.Count == 0)
        {
            await ErrorHandler.MostrarErrorUsuario("No se seleccionaron productos válidos para dividir.");
            return;
        }

        var payload = splitPlan.Request;
        var payloadSnapshot = new
        {
            payload.itemIds,
            payload.serviceType,
            payload.tableId,
            payload.SendTableId,
            payload.note,
            payload.covers
        };

        var finalItemIds = new List<int>();

        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await ErrorHandler.MostrarErrorUsuario("Sin conexión a Internet.");
                return;
            }

            IsLoading = true;

            foreach (var selection in splitPlan.Items)
            {
                if (selection.SelectedQuantity <= 0)
                    continue;

                int itemId;
                if (!selection.AllowPartial || selection.SelectedQuantity >= selection.OriginalQuantity)
                {
                    itemId = selection.ItemId;
                }
                else
                {
                    itemId = await EnsurePartialItemAsync(selection);
                }

                if (!finalItemIds.Contains(itemId))
                    finalItemIds.Add(itemId);
            }

            if (finalItemIds.Count == 0)
            {
                await ErrorHandler.MostrarErrorUsuario("No se pudieron preparar los productos para dividir.");
                return;
            }

            payload.itemIds = finalItemIds;

            var response = await _ordersApi.SplitAsync(OrderId, payload);
            await LoadOrderAsync();

            if (response == null)
            {
                await ErrorHandler.MostrarErrorUsuario("Se solicitó dividir el pedido, pero no se obtuvo confirmación del servidor.");
                return;
            }

            var openNew = await DisplayAlert(
                "Pedido dividido",
                $"Se creó la orden {response.code}. ¿Deseas abrirla ahora?",
                "Ver pedido",
                "Seguir aquí");

            if (openNew)
                await Shell.Current.GoToAsync($"{nameof(OrderDetailPage)}?orderId={response.newOrderId}");
        }
        catch (HttpRequestException ex)
        {
            var message = ErrorHandler.ObtenerMensajeHttp(new HttpResponseMessage(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), ex.Message);
#if DEBUG
            var payloadJson = JsonSerializer.Serialize(payloadSnapshot, new JsonSerializerOptions { WriteIndented = true });
            var debugInfo = $"Payload enviado:\n{payloadJson}\n\nRespuesta del servidor:\n{ex.Message}";
            await Clipboard.Default.SetTextAsync(debugInfo);
            await ErrorHandler.MostrarErrorTecnico(new Exception("Detalle copiado al portapapeles."), "Órdenes – Dividir pedido (debug)");
#endif
            await ErrorHandler.MostrarErrorUsuario(message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Órdenes – Dividir pedido");
        }
        finally
        {
            IsLoading = false;
        }
    }

    async Task<int> EnsurePartialItemAsync(OrderSplitPopup.ItemSelection selection)
    {
        var item = FindOrderItemById(selection.ItemId);
        if (item == null)
            throw new InvalidOperationException($"No se encontró el producto #{selection.ItemId} para dividir.");

        var moveQty = selection.SelectedQuantity;
        var maxQty = item.quantity;
        if (moveQty <= 0 || moveQty >= maxQty)
            return item.id;

        var stayQty = maxQty - moveQty;

        await _ordersApi.UpdateItemAsync(item.id, new UpdateOrderItemDto
        {
            quantity = moveQty
        });

        if (stayQty > 0)
        {
            var cloneDto = BuildCloneAddItemDto(item, stayQty);
            await _ordersApi.AddItemAsync(OrderId, cloneDto);
        }

        return item.id;
    }

    static AddOrderItemDto BuildCloneAddItemDto(OrderItemDTO item, int quantity)
    {
        return new AddOrderItemDto
        {
            productId = item.productId,
            variantId = item.variantId,
            quantity = quantity,
            notes = item.notes,
            modifiers = item.modifiers?
                .Select(m => new OrderModifierSelectionInput { optionId = m.optionId, quantity = m.quantity })
                .ToList() ?? new List<OrderModifierSelectionInput>()
        };
    }

    OrderItemDTO? FindOrderItemById(int itemId)
    {
        if (_currentOrder?.items == null)
            return null;

        foreach (var item in _currentOrder.items)
        {
            var found = FindRecursive(item, itemId);
            if (found != null)
                return found;
        }

        return null;

        static OrderItemDTO? FindRecursive(OrderItemDTO node, int targetId)
        {
            if (node.id == targetId)
                return node;
            if (node.childItems == null)
                return null;
            foreach (var child in node.childItems)
            {
                var result = FindRecursive(child, targetId);
                if (result != null)
                    return result;
            }
            return null;
        }
    }

    record AdvancedEditorContext(
        TakeOrderPage.MenuItemVm BaseItem,
        IReadOnlyList<TakeOrderPage.MenuItemVm> Variants,
        IReadOnlyList<ModifierGroupDTO> ModifierGroups,
        TakeOrderPage.CartEntry ExistingEntry,
        Func<int, Task<IReadOnlyList<VariantModifierGroupLinkDTO>>>? VariantRulesLoader);

    async Task<AdvancedEditorContext?> BuildAdvancedEditorContextAsync(OrderItemDTO item, bool allowVariantSelection = false)
    {
        if (item.productId <= 0)
            return null;
        MenusApi.ProductSummaryDto? product;
        try
        {
            product = await _menusApi.GetProductAsync(item.productId);
        }
        catch
        {
            return null;
        }

        if (product == null)
            return null;

        var section = new MenusApi.MenuPublicSectionDto { id = -1, name = "Producto" };
        var productRef = new MenusApi.MenuPublicProductReference
        {
            id = product.id,
            name = product.name,
            type = product.type,
            description = null,
            priceCents = item.basePriceCents > 0 ? item.basePriceCents : product.priceCents,
            isActive = product.isActive,
            isAvailable = product.isAvailable ?? true,
            imageUrl = null,
            hasImage = false
        };

        MenusApi.ProductVariantDto? variantSummary = null;
        if (item.variantId.HasValue)
        {
            variantSummary = product.variants?.FirstOrDefault(v => v.id == item.variantId.Value);
            if (variantSummary == null)
                return null;
        }

        var selectedVm = BuildMenuItemVm(section, productRef, variantSummary, item);
        if (selectedVm == null)
            return null;

        var variants = new List<TakeOrderPage.MenuItemVm> { selectedVm };
        if (allowVariantSelection && product.variants != null && product.variants.Count > 0)
        {
            variants.Clear();
            variants.Add(BuildMenuItemVm(section, productRef, null, item) ?? selectedVm);
            foreach (var variant in product.variants)
            {
                var variantOrder = new OrderItemDTO
                {
                    id = 0,
                    productId = product.id,
                    variantId = variant.id,
                    quantity = item.quantity,
                    basePriceCents = variant.priceCents ?? product.priceCents ?? item.basePriceCents,
                    nameSnapshot = variant.name ?? product.name,
                    modifiers = new List<OrderItemModifierDTO>(),
                    childItems = new List<OrderItemDTO>()
                };
                var variantVm = BuildMenuItemVm(section, productRef, variant, variantOrder);
                if (variantVm != null)
                    variants.Add(variantVm);
            }
        }
        var modifierGroups = await GetModifierGroupsForProductAsync(product.id);
        var cartModifiers = BuildCartModifierSelections(item, modifierGroups);
        var configureResult = new ConfigureMenuItemResult(
            selectedVm,
            selectedVm,
            item.quantity,
            item.notes,
            cartModifiers,
            Array.Empty<TakeOrderPage.ComboChildSelection>(),
            Guid.NewGuid());
        var cartEntry = new TakeOrderPage.CartEntry(configureResult);

        Func<int, Task<IReadOnlyList<VariantModifierGroupLinkDTO>>>? variantRulesLoader = null;
        if (allowVariantSelection || (product.variants != null && product.variants.Count > 0) || item.variantId.HasValue)
        {
            variantRulesLoader = async variantId =>
            {
                try { return await _menusApi.GetVariantModifierGroupsAsync(variantId); }
                catch { return Array.Empty<VariantModifierGroupLinkDTO>(); }
            };
        }

        return new AdvancedEditorContext(selectedVm, variants, modifierGroups, cartEntry, variantRulesLoader);
    }

    TakeOrderPage.MenuItemVm? BuildMenuItemVm(
        MenusApi.MenuPublicSectionDto section,
        MenusApi.MenuPublicProductReference productRef,
        MenusApi.ProductVariantDto? variant,
        OrderItemDTO orderItem)
    {
        MenusApi.MenuPublicVariantReference? variantRef = null;
        string refType = "PRODUCT";
        int refId = productRef.id;

        if (variant != null)
        {
            variantRef = new MenusApi.MenuPublicVariantReference
            {
                id = variant.id,
                name = variant.name,
                priceCents = variant.priceCents,
                isActive = variant.isActive ?? true,
                isAvailable = variant.isAvailable ?? true,
                product = productRef,
                imageUrl = variant.imageUrl,
                hasImage = variant.hasImage ?? false,
                modifierGroups = variant.modifierGroups ?? new List<VariantModifierGroupLinkDTO>()
            };
            refType = "VARIANT";
            refId = variant.id;
        }

        var menuItemDto = new MenusApi.MenuPublicItemDto
        {
            id = refId,
            sectionId = section.id,
            refType = refType,
            refId = refId,
            displayName = orderItem.nameSnapshot ?? variant?.name ?? productRef.name,
            displayPriceCents = orderItem.basePriceCents,
            position = 0,
            isFeatured = false,
            isActive = productRef.isActive,
            @ref = new MenusApi.MenuPublicReferenceDto
            {
                kind = refType,
                product = productRef,
                variant = variantRef,
                components = new List<MenusApi.MenuPublicComboComponent>()
            }
        };

        return TakeOrderPage.MenuItemVm.From(section, menuItemDto);
    }

    async Task<List<ModifierGroupDTO>> GetModifierGroupsForProductAsync(int productId)
    {
        if (_modifierGroupsCache.TryGetValue(productId, out var cached))
            return cached;

        try
        {
            var links = await _modifiersApi.GetGroupsByProductAsync(productId);
            var groups = links
                .Where(l => l.group != null && l.group.isActive)
                .OrderBy(l => l.position)
                .Select(l => l.group!)
                .ToList();
            _modifierGroupsCache[productId] = groups;
            return groups;
        }
        catch
        {
            return new List<ModifierGroupDTO>();
        }
    }

    async Task RefundCurrentOrderAsync()
    {
        if (_currentOrder == null || !CanRefund || _isProcessingRefund)
            return;

        var popup = new RefundOrderPopup();
        if (await this.ShowPopupAsync(popup) is not RefundOrderRequest request)
            return;

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            await ErrorHandler.MostrarErrorUsuario("Sin conexión a Internet.");
            return;
        }

        try
        {
            _isProcessingRefund = true;
            IsLoading = true;
            await _ordersApi.RefundOrderAsync(_currentOrder.id, request);
            await LoadOrderAsync();
            await DisplayAlert("Reembolso registrado", "El pedido fue marcado como reembolsado.", "OK");
        }
        catch (HttpRequestException ex)
        {
            var message = ErrorHandler.ObtenerMensajeHttp(new HttpResponseMessage(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), ex.Message);
            await ErrorHandler.MostrarErrorUsuario(message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Órdenes – Reembolso");
        }
        finally
        {
            _isProcessingRefund = false;
            IsLoading = false;
        }
    }

    static IReadOnlyList<TakeOrderPage.CartModifierSelection> BuildCartModifierSelections(
        OrderItemDTO item,
        IReadOnlyList<ModifierGroupDTO> groups)
    {
        var selections = new List<TakeOrderPage.CartModifierSelection>();
        var modifiers = item.modifiers ?? new List<OrderItemModifierDTO>();
        var assignedOptions = new HashSet<int>();

        foreach (var group in groups)
        {
            var options = new List<TakeOrderPage.ModifierOptionSelection>();
            if (group.options != null)
            {
                foreach (var optionDefinition in group.options)
                {
                    var match = modifiers.FirstOrDefault(m => m.optionId == optionDefinition.id);
                    if (match == null) continue;
                    assignedOptions.Add(match.optionId);
                    var price = optionDefinition.priceExtraCents.ToCurrency();
                    options.Add(new TakeOrderPage.ModifierOptionSelection(
                        optionDefinition.id,
                        optionDefinition.name ?? match.nameSnapshot ?? $"Opción {optionDefinition.id}",
                        price,
                        Math.Max(1, match.quantity)));
                }
            }

            if (options.Count > 0)
                selections.Add(new TakeOrderPage.CartModifierSelection(group.id, group.name, options));
        }

        var leftovers = modifiers.Where(m => !assignedOptions.Contains(m.optionId)).ToList();
        if (leftovers.Count > 0)
        {
            var extraOptions = leftovers.Select(m =>
                new TakeOrderPage.ModifierOptionSelection(
                    m.optionId,
                    m.nameSnapshot ?? $"Opción {m.optionId}",
                    m.priceExtraCents.ToCurrency(),
                    Math.Max(1, m.quantity)));
            selections.Add(new TakeOrderPage.CartModifierSelection(0, "Ingredientes", extraOptions));
        }

        return selections;
    }

    static List<OrderModifierSelectionInput> BuildModifierPayload(ConfigureMenuItemResult result)
        => BuildModifierPayload(result?.Modifiers);

    static List<OrderModifierSelectionInput> BuildModifierPayload(IEnumerable<TakeOrderPage.CartModifierSelection>? selections)
    {
        var list = new List<OrderModifierSelectionInput>();
        if (selections == null)
            return list;

        foreach (var group in selections)
        {
            foreach (var opt in group.Options)
            {
                list.Add(new OrderModifierSelectionInput
                {
                    optionId = opt.OptionId,
                    quantity = opt.Quantity
                });
            }
        }

        return list;
    }

    static List<ComboChildSelectionInput>? BuildComboChildrenPayload(IReadOnlyList<TakeOrderPage.ComboChildSelection>? children)
    {
        if (children == null || children.Count == 0)
            return null;

        var list = new List<ComboChildSelectionInput>();
        foreach (var child in children)
        {
            list.Add(new ComboChildSelectionInput
            {
                productId = child.ProductId,
                variantId = child.VariantId,
                quantity = Math.Max(1, child.Quantity),
                notes = string.IsNullOrWhiteSpace(child.Notes) ? null : child.Notes.Trim(),
                modifiers = BuildModifierPayload(child.Modifiers)
            });
        }

        return list;
    }

    static AddOrderItemDto? BuildAddOrderItemDto(ConfigureMenuItemResult result)
    {
        var productId = result.SelectedItem.ProductId ?? result.BaseItem.ProductId;
        if (!productId.HasValue || productId.Value <= 0)
            return null;

        return new AddOrderItemDto
        {
            productId = productId.Value,
            variantId = result.SelectedItem.VariantId,
            quantity = Math.Max(1, result.Quantity),
            notes = string.IsNullOrWhiteSpace(result.Notes) ? null : result.Notes.Trim(),
            modifiers = BuildModifierPayload(result),
            children = BuildComboChildrenPayload(result.Children)
        };
    }

    string BuildSelectionDebugReport(string actionDescription)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Modo debug – {actionDescription}");
        sb.AppendLine($"Orden #{_currentOrder?.id} – {DateTime.Now}");
        if (_currentOrder?.items == null)
        {
            sb.AppendLine("Sin items.");
            return sb.ToString();
        }

        int index = 1;
        foreach (var item in _currentOrder.items.Where(i => i.parentItemId == null))
        {
            AppendDebugEntry(sb, item, 0, index++);
        }

        return sb.ToString();
    }

    void AppendDebugEntry(StringBuilder sb, OrderItemDTO item, int level, int index)
    {
        var indent = new string(' ', level * 2);
        var type = item.childItems != null && item.childItems.Count > 0
            ? "Combo"
            : item.parentItemId.HasValue ? "Hijo" : "Producto";
        sb.AppendLine($"{indent}[{index}] {type} ID={item.id} Parent={item.parentItemId?.ToString() ?? "-"} Qty={item.quantity} Name={item.nameSnapshot ?? item.product?.name}");

        if (item.childItems == null) return;

        int childIndex = 1;
        foreach (var child in item.childItems)
        {
            AppendDebugEntry(sb, child, level + 1, childIndex++);
        }
    }

    public class TimelineEntryVm
    {
        public string Title { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;

        public static TimelineEntryVm Create(string title, DateTime? timestamp)
        {
            var value = timestamp?.ToLocalTime().ToString("dd/MM HH:mm") ?? "-";
            return new TimelineEntryVm { Title = title, Value = value };
        }
    }

    public class OrderLineVm
    {
        public string QuantityText { get; init; } = "1×";
        public string Title { get; init; } = string.Empty;
        public string Detail { get; init; } = string.Empty;
        public string LineTotal { get; init; } = "$0.00";
        public string StatusBadge { get; init; } = string.Empty;
        public Color StatusColor { get; init; } = Colors.Gray;
        public Thickness Margin { get; init; } = new(0);
        public OrderItemDTO? Source { get; init; }

        public static OrderLineVm From(OrderItemDTO item, int level)
        {
            var detailParts = new List<string>();
            var isCombo = string.Equals(item.product?.type, "COMBO", StringComparison.OrdinalIgnoreCase);
            if (isCombo)
                detailParts.Add("Combo");

            if (!string.IsNullOrWhiteSpace(item.variantNameSnapshot))
                detailParts.Add(item.variantNameSnapshot);

            if (item.modifiers != null && item.modifiers.Count > 0)
            {
                var mods = string.Join(", ", item.modifiers.Select(m =>
                {
                    var qty = m.quantity > 1 ? $"{m.quantity}× " : string.Empty;
                    var price = m.priceExtraCents > 0 ? $" (+{FormatCurrency(m.priceExtraCents)})" : string.Empty;
                    return $"{qty}{m.nameSnapshot ?? $"Opción {m.optionId}"}{price}";
                }));
                detailParts.Add($"Ingredientes: {mods}");
            }

            if (!string.IsNullOrWhiteSpace(item.notes))
                detailParts.Add($"Nota: {item.notes}");

            var statusBadge = item.status switch
            {
                "NEW" => "Nuevo",
                "IN_PROGRESS" => "Preparando",
                "READY" => "Listo",
                "SERVED" => "Servido",
                "CANCELED" => "Cancelado",
                _ => item.status
            };

            var statusColor = item.status switch
            {
                "NEW" => Color.FromArgb("#0288D1"),
                "IN_PROGRESS" => Color.FromArgb("#FB8C00"),
                "READY" => Color.FromArgb("#388E3C"),
                "SERVED" => Color.FromArgb("#455A64"),
                "CANCELED" => Color.FromArgb("#E53935"),
                _ => Colors.Gray
            };

            var lineTotal = item.totalPriceCents == 0 && item.parentItemId.HasValue
                ? "Incluido"
                : FormatCurrency(item.totalPriceCents);

            return new OrderLineVm
            {
                QuantityText = $"{item.quantity}×",
                Title = item.nameSnapshot ?? item.product?.name ?? $"Producto {item.productId}",
                Detail = detailParts.Count == 0 ? " " : string.Join(" · ", detailParts),
                LineTotal = lineTotal,
                StatusBadge = statusBadge,
                StatusColor = statusColor,
                Margin = new Thickness(level * 18, 0, 0, 0),
                Source = item
            };
        }
    }

    public class OrderPaymentVm
    {
        public string Method { get; init; } = string.Empty;
        public string Amount { get; init; } = "$0.00";
        public string Detail { get; init; } = string.Empty;
        public string PaidAt { get; init; } = string.Empty;

        public static OrderPaymentVm From(OrderPaymentDTO dto)
        {
            var method = dto.method switch
            {
                "CASH" => "Efectivo",
                "CARD" => "Tarjeta",
                "TRANSFER" => "Transferencia",
                "OTHER" => "Otro",
                _ => dto.method
            };

            var detailParts = new List<string>();
            if (dto.tipCents > 0)
                detailParts.Add($"Propina {FormatCurrency(dto.tipCents)}");
            if (!string.IsNullOrWhiteSpace(dto.note))
                detailParts.Add(dto.note);

            return new OrderPaymentVm
            {
                Method = method,
                Amount = FormatCurrency(dto.amountCents),
                Detail = detailParts.Count == 0 ? " " : string.Join(" · ", detailParts),
                PaidAt = dto.paidAt?.ToLocalTime().ToString("dd/MM HH:mm") ?? string.Empty
            };
        }
    }

    

    static bool AreAllItemsReady(IEnumerable<OrderItemDTO>? items)
    {
        if (items == null)
            return true;

        foreach (var item in items)
        {
            if (!IsItemReadyForPayment(item))
                return false;
        }
        return true;
    }

    static bool IsItemReadyForPayment(OrderItemDTO item)
    {
        var status = item.status?.ToUpperInvariant() ?? string.Empty;
        var allowed = status == "READY" || status == "SERVED" || status == "CANCELED";
        if (!allowed)
            return false;

        if (item.childItems == null || item.childItems.Count == 0)
            return true;

        foreach (var child in item.childItems)
        {
            if (!IsItemReadyForPayment(child))
                return false;
        }

        return true;
    }
}
