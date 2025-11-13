using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using Imdeliceapp.Helpers;
using Imdeliceapp.Models;
using Imdeliceapp.Popups;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;
using System.Runtime.CompilerServices;

namespace Imdeliceapp.Pages;

public partial class KdsPage : ContentPage
{
    readonly OrdersApi _ordersApi = new();
    readonly List<StatusFilter> _statusFilters = new()
    {
        new StatusFilter("Pendientes", new[] { "NEW", "IN_PROGRESS" }),
        new StatusFilter("Listos", new[] { "READY" }),
        new StatusFilter("Cancelados", new[] { "CANCELED" }),
        new StatusFilter("Todos", Array.Empty<string>())
    };

    readonly List<ServiceFilter> _serviceFilters = new()
    {
        new ServiceFilter("Todos", null),
        new ServiceFilter("En mesa", "DINE_IN"),
        new ServiceFilter("Para llevar", "TAKEAWAY"),
        new ServiceFilter("Delivery", "DELIVERY")
    };

    public ObservableCollection<KdsTicketVm> Tickets { get; } = new();
    bool _isLoading;
    readonly TimeSpan _autoRefreshInterval = TimeSpan.FromMinutes(1);
    readonly TimeSpan _countdownInterval = TimeSpan.FromSeconds(1);
    IDispatcherTimer? _autoRefreshTimer;
    IDispatcherTimer? _countdownTimer;
    bool _isStatusUpdateInProgress;

    public KdsPage()
    {
        InitializeComponent();
        BindingContext = this;
        StatusPicker.ItemsSource = _statusFilters;
        StatusPicker.SelectedIndex = 0;
        ServicePicker.ItemsSource = _serviceFilters;
        ServicePicker.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTicketsAsync();
        StartAutoRefreshTimer();
        StartCountdownTimer();
    }

    protected override void OnDisappearing()
    {
        StopAutoRefreshTimer();
        StopCountdownTimer();
        base.OnDisappearing();
    }

    async Task LoadTicketsAsync()
    {
        if (_isLoading)
            return;

        try
        {
            _isLoading = true;
            RefreshControl.IsRefreshing = true;

            var statusFilter = StatusPicker.SelectedItem as StatusFilter;
            var serviceFilter = ServicePicker.SelectedItem as ServiceFilter;

            var query = new KdsQuery
            {
                Statuses = statusFilter?.Codes,
                ServiceType = serviceFilter?.ServiceType,
                TzOffsetMinutes = OrdersApi.GetLocalTimezoneOffsetMinutes()
            };

            var tickets = await _ordersApi.GetKdsTicketsAsync(query);
            Tickets.Clear();
            foreach (var ticket in tickets
                         .OrderBy(t => t.openedAt ?? DateTime.MaxValue))
            {
                var vm = new KdsTicketVm(ticket, UpdateItemStatusAsync);
                vm.UpdateCountdown(DateTime.Now);
                Tickets.Add(vm);
            }
            RefreshTicketCountdowns();
        }
        catch (HttpRequestException ex)
        {
            var message = ErrorHandler.ObtenerMensajeHttp(new System.Net.Http.HttpResponseMessage(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), ex.Message);
            await ErrorHandler.MostrarErrorUsuario(message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "KDS – Listado");
        }
        finally
        {
            _isLoading = false;
            RefreshControl.IsRefreshing = false;
        }
    }

    void StartAutoRefreshTimer()
    {
        StopAutoRefreshTimer();
        if (Dispatcher == null) return;

        _autoRefreshTimer = Dispatcher.CreateTimer();
        _autoRefreshTimer.Interval = _autoRefreshInterval;
        _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;
        _autoRefreshTimer.Start();
    }

    void StopAutoRefreshTimer()
    {
        if (_autoRefreshTimer == null) return;
        _autoRefreshTimer.Tick -= AutoRefreshTimer_Tick;
        _autoRefreshTimer.Stop();
        _autoRefreshTimer = null;
    }

    void StartCountdownTimer()
    {
        StopCountdownTimer();
        if (Dispatcher == null) return;

        _countdownTimer = Dispatcher.CreateTimer();
        _countdownTimer.Interval = _countdownInterval;
        _countdownTimer.Tick += CountdownTimer_Tick;
        _countdownTimer.Start();
    }

    void StopCountdownTimer()
    {
        if (_countdownTimer == null) return;
        _countdownTimer.Tick -= CountdownTimer_Tick;
        _countdownTimer.Stop();
        _countdownTimer = null;
    }

    async void AutoRefreshTimer_Tick(object? sender, EventArgs e)
    {
        await LoadTicketsAsync();
    }

    async void RefreshToolbar_Clicked(object sender, EventArgs e)
    {
        await LoadTicketsAsync();
    }

    async void RefreshControl_Refreshing(object sender, EventArgs e)
    {
        await LoadTicketsAsync();
    }

    async void FilterChanged(object sender, EventArgs e)
    {
        await LoadTicketsAsync();
    }

    async Task UpdateItemStatusAsync(int itemId, string newStatus)
    {
        if (_isStatusUpdateInProgress)
            return;

        try
        {
            _isStatusUpdateInProgress = true;
            await _ordersApi.UpdateItemStatusAsync(itemId, new UpdateOrderItemStatusDto { status = newStatus });
            await LoadTicketsAsync();
        }
        catch (HttpRequestException ex)
        {
            var message = ErrorHandler.ObtenerMensajeHttp(new System.Net.Http.HttpResponseMessage(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), ex.Message);
            await ErrorHandler.MostrarErrorUsuario(message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "KDS – Actualizar estado de item");
        }
        finally
        {
            _isStatusUpdateInProgress = false;
        }
    }

    void CountdownTimer_Tick(object? sender, EventArgs e) => RefreshTicketCountdowns();

    void RefreshTicketCountdowns()
    {
        var now = DateTime.Now;
        foreach (var ticket in Tickets)
            ticket.UpdateCountdown(now);
    }

    async void TicketCard_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not KdsTicketVm ticket)
            return;

        await this.ShowPopupAsync(new KdsTicketDetailPopup(ticket));
    }

    record StatusFilter(string Label, IList<string> Codes)
    {
        public override string ToString() => Label;
    }

    record ServiceFilter(string Label, string? ServiceType)
    {
        public override string ToString() => Label;
    }

    public class KdsTicketVm : INotifyPropertyChanged
    {
        public KdsTicketVm(KdsTicketDTO dto, Func<int, string, Task> statusUpdater)
        {
            Source = dto;
            OrderId = dto.orderId;
            Code = CleanCode(dto.code);
            Subtitle = BuildSubtitle(dto);
            StatusBadge = TranslateStatus(dto.status);
            StatusColor = GetStatusColor(dto.status);
            Note = dto.note;
            HasNote = !string.IsNullOrWhiteSpace(dto.note);
            Items = dto.items?.Select(i => KdsItemVm.From(i, statusUpdater)).ToList() ?? new List<KdsItemVm>();
            ServiceSummary = BuildServiceSummary(dto);
            TableLabel = dto.table?.name;
            HasTable = !string.IsNullOrWhiteSpace(TableLabel);
            ServiceType = dto.serviceType ?? string.Empty;
            SourceLabel = dto.source ?? "POS";
            _openedAt = dto.openedAt?.ToLocalTime();
            _deadline = CalculateDeadline(dto);
            UpdateCountdown(DateTime.Now);
        }

        public KdsTicketDTO Source { get; }
        public int OrderId { get; }
        public string Code { get; }
        public string Subtitle { get; }
        public string StatusBadge { get; }
        public Color StatusColor { get; }
        public string? Note { get; }
        public bool HasNote { get; }
        public List<KdsItemVm> Items { get; }
        public string ServiceSummary { get; }
        string _timingSummary = string.Empty;
        public string TimingSummary
        {
            get => _timingSummary;
            private set
            {
                if (_timingSummary == value) return;
                _timingSummary = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasTimingSummary));
            }
        }

        public bool HasTimingSummary => !string.IsNullOrWhiteSpace(TimingSummary);
        public string? TableLabel { get; }
        public bool HasTable { get; }
        public string ServiceType { get; }
        public string SourceLabel { get; }
        readonly DateTime? _openedAt;
        readonly DateTime? _deadline;

        static string BuildSubtitle(KdsTicketDTO dto)
        {
            var parts = new List<string>();
            if (dto.table != null && !string.IsNullOrWhiteSpace(dto.table.name))
                parts.Add(dto.table.name);
            if (!string.IsNullOrWhiteSpace(dto.serviceType))
                parts.Add(TranslateServiceType(dto.serviceType));
            if (!string.IsNullOrWhiteSpace(dto.source))
                parts.Add(dto.source);

            return parts.Count == 0 ? "Ticket" : string.Join(" · ", parts);
        }

        static string BuildServiceSummary(KdsTicketDTO dto)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(dto.serviceType))
                parts.Add(TranslateServiceType(dto.serviceType));
            if (!string.IsNullOrWhiteSpace(dto.source))
                parts.Add(dto.source);
            return parts.Count == 0 ? "Sin canal" : string.Join(" · ", parts);
        }

        static DateTime? CalculateDeadline(KdsTicketDTO dto)
        {
            if (!dto.prepEtaMinutes.HasValue || dto.prepEtaMinutes.Value <= 0)
                return null;

            if (!dto.openedAt.HasValue)
                return null;

            var reference = dto.openedAt.Value.Kind switch
            {
                DateTimeKind.Utc => dto.openedAt.Value.ToLocalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(dto.openedAt.Value, DateTimeKind.Local),
                _ => dto.openedAt.Value
            };

            return reference + TimeSpan.FromMinutes(dto.prepEtaMinutes.Value);
        }

        static string TranslateStatus(string status) => status?.ToUpperInvariant() switch
        {
            "OPEN" => "Activo",
            "HOLD" => "En espera",
            "READY" => "Listo",
            "IN_PROGRESS" => "Preparando",
            "NEW" => "Nuevo",
            "CANCELED" => "Cancelado",
            "CLOSED" => "Cerrado",
            _ => status ?? string.Empty
        };

        static string TranslateServiceType(string serviceType) => serviceType?.ToUpperInvariant() switch
        {
            "DINE_IN" => "En mesa",
            "TAKEAWAY" => "Para llevar",
            "DELIVERY" => "Delivery",
            _ => serviceType ?? string.Empty
        };

        static Color GetStatusColor(string status) => status?.ToUpperInvariant() switch
        {
            "READY" => Color.FromArgb("#2E7D32"),
            "CANCELED" => Color.FromArgb("#C62828"),
            "HOLD" => Color.FromArgb("#F9A825"),
            _ => Color.FromArgb("#0277BD")
        };

        public void UpdateCountdown(DateTime now)
        {
            var parts = new List<string>();
            if (_openedAt.HasValue)
                parts.Add($"Inicio {_openedAt.Value:HH:mm}");

            if (_deadline.HasValue)
            {
                var remaining = _deadline.Value - now;
                parts.Add(remaining <= TimeSpan.Zero
                    ? "ETA vencido"
                    : $"ETA {FormatRemaining(remaining)}");
            }

            TimingSummary = parts.Count == 0 ? string.Empty : string.Join(" • ", parts);
        }

        static string CleanCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return "Ticket";
            var index = code.IndexOf(" - ", StringComparison.Ordinal);
            return index > 0 ? code[..index] : code;
        }

        static string FormatRemaining(TimeSpan remaining)
        {
            if (remaining.TotalHours >= 1)
                return $"{(int)remaining.TotalHours}h {remaining.Minutes:D2}m";
            return $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class KdsItemVm
    {
        public static KdsItemVm From(KdsItemDTO dto, Func<int, string, Task> statusUpdater)
        {
            return new KdsItemVm(dto, statusUpdater);
        }

        KdsItemVm(KdsItemDTO dto, Func<int, string, Task> statusUpdater)
        {
            Id = dto.id;
            Title = $"{dto.quantity}× {dto.name ?? $"Item #{dto.id}"}";
            if (!string.IsNullOrWhiteSpace(dto.variantName))
                Title += $" · {dto.variantName}";

            StatusCode = string.IsNullOrWhiteSpace(dto.status) ? "NEW" : dto.status.ToUpperInvariant();
            StatusLabel = StatusCode switch
            {
                "NEW" => "Nuevo",
                "IN_PROGRESS" => "Preparando",
                "READY" => "Listo",
                "SERVED" => "Servido",
                "CANCELED" => "Cancelado",
                _ => StatusCode
            };

            var detailParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(dto.notes))
                detailParts.Add($"Nota: {dto.notes}");
            if (dto.modifiers?.Count > 0)
            {
                var mods = string.Join(", ", dto.modifiers.Select(m =>
                    m.quantity > 1 ? $"{m.quantity}× {m.name}" : m.name));
                detailParts.Add($"Ingredientes: {mods}");
            }

            Detail = detailParts.Count == 0 ? null : string.Join(" • ", detailParts);
            HasDetail = !string.IsNullOrWhiteSpace(Detail);

            IsComboParent = dto.isComboParent || (dto.children?.Count > 0);
            Actions = BuildActions(this, statusUpdater);
            HasActions = Actions.Count > 0;

            Children = dto.children?.Select(child => new KdsItemVm(child, statusUpdater)).ToList() ?? new List<KdsItemVm>();
            HasChildren = Children.Count > 0;
        }

        static List<ItemStatusActionVm> BuildActions(KdsItemVm item, Func<int, string, Task> statusUpdater)
        {
            var actions = new List<ItemStatusActionVm>();
            foreach (var (code, label) in GetTransitions(item.StatusCode))
                actions.Add(new ItemStatusActionVm(label, () => item.TryExecuteTransitionAsync(code, statusUpdater)));
            return actions;
        }

        static IEnumerable<(string code, string label)> GetTransitions(string? status)
        {
            return (status?.ToUpperInvariant()) switch
            {
                "NEW" => new[] { ("IN_PROGRESS", "Preparar"), ("CANCELED", "Cancelar") },
                "IN_PROGRESS" => new[] { ("READY", "Listo"), ("CANCELED", "Cancelar") },
                "READY" => new[] { ("SERVED", "Servido"), ("CANCELED", "Cancelar") },
                _ => Array.Empty<(string, string)>()
            };
        }

        public int Id { get; }
        public string Title { get; }
        public string StatusCode { get; }
        public string StatusLabel { get; }
        public string? Detail { get; }
        public bool HasDetail { get; }
        public IReadOnlyList<ItemStatusActionVm> Actions { get; }
        public bool HasActions { get; }
        public List<KdsItemVm> Children { get; }
        public bool HasChildren { get; }
        public bool IsComboParent { get; }

        Task TryExecuteTransitionAsync(string targetStatus, Func<int, string, Task> statusUpdater)
        {
            if (!NeedsChildValidation(targetStatus) || ValidateChildStatuses(targetStatus, out var warning))
                return statusUpdater(Id, targetStatus);

            return ShowComboAlertAsync(warning);
        }

        bool NeedsChildValidation(string targetStatus)
        {
            if (!IsComboParent || !HasChildren)
                return false;
            var upper = (targetStatus ?? string.Empty).ToUpperInvariant();
            return upper == "READY" || upper == "SERVED";
        }

        bool ValidateChildStatuses(string targetStatus, out string? message)
        {
            message = null;
            if (!NeedsChildValidation(targetStatus))
                return true;

            var requiredRank = GetStatusRank(targetStatus);
            if (requiredRank < 0)
                return true;

            var blocking = Children.Where(child => GetStatusRank(child.StatusCode) < requiredRank).ToList();
            if (blocking.Count == 0)
                return true;

            message = requiredRank == GetStatusRank("READY")
                ? "Este combo aún tiene componentes pendientes. Marca cada componente como \"Listo\" antes de completar el combo."
                : "Este combo aún tiene componentes sin servir. Marca cada componente como \"Servido\" para poder cerrar el combo.";
            return false;
        }

        static async Task ShowComboAlertAsync(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (Application.Current?.MainPage == null)
                return;

            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Application.Current.MainPage.DisplayAlert("Combo incompleto", message, "Entendido"));
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

        public class ItemStatusActionVm
        {
            public ItemStatusActionVm(string label, Func<Task> execute)
            {
                Label = label;
                Command = new Command(async () => await execute());
            }

            public string Label { get; }
            public Command Command { get; }
        }
    }
}
