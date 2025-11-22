using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net;
using Imdeliceapp.Helpers;
using Imdeliceapp.Models;
using CommunityToolkit.Maui.Views;
using Imdeliceapp.Popups;
using Imdeliceapp.Services;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Networking;

namespace Imdeliceapp.Pages;

public partial class OrdersPage : ContentPage
{
    readonly OrdersApi _ordersApi = new();
    readonly ObservableCollection<OrderListItem> _orders = new();
    readonly List<OrderListItem> _all = new();
    readonly List<OrderSummaryDTO> _rawOrders = new();
    List<TableDTO> _tablesCache = new();

    bool _initialized;
    DateTime? _fromDate;
    DateTime? _toDate;
    bool _useDateFilter;
    string _searchText = string.Empty;

    public ObservableCollection<OrderListItem> Orders => _orders;

    public List<FilterOption> StatusOptions { get; }
    public List<FilterOption> ServiceTypeOptions { get; }
    public List<FilterOption> SourceOptions { get; }

    FilterOption? _selectedStatusOption;
    FilterOption? _selectedServiceOption;
    FilterOption? _selectedSourceOption;

    string _filtersSummary = string.Empty;
    public string FiltersSummary
    {
        get => _filtersSummary;
        private set
        {
            if (_filtersSummary == value) return;
            _filtersSummary = value;
            OnPropertyChanged();
        }
    }

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

    string _emptyMessage = "No hay órdenes";
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

    int? _tableIdFilter;

    ToolbarItem? _newOrderToolbar;

    public OrdersPage()
    {
        InitializeComponent();

        StatusOptions = new List<FilterOption>
        {
            new("En proceso", "OPEN,HOLD"),
            new("Todos", null),
            new("Borrador", "DRAFT"),
            new("Abiertos", "OPEN"),
            new("En pausa", "HOLD"),
            new("Cerrados", "CLOSED"),
            new("Cancelados", "CANCELED")
        };
        ServiceTypeOptions = new List<FilterOption>
        {
            new("Todos", null),
            new("En mesa", "DINE_IN"),
            new("Para llevar", "TAKEAWAY"),
            new("Delivery", "DELIVERY")
        };
        SourceOptions = new List<FilterOption>
        {
            new("Todos", null),
            new("POS", "POS"),
            new("Uber", "UBER"),
            new("DiDi", "DIDI"),
            new("Rappi", "RAPPI")
        };

        BindingContext = this;
        OnPropertyChanged(nameof(StatusOptions));
        OnPropertyChanged(nameof(ServiceTypeOptions));
        OnPropertyChanged(nameof(SourceOptions));

        _selectedStatusOption = StatusOptions[0];
        _selectedServiceOption = ServiceTypeOptions[0];
        _selectedSourceOption = SourceOptions[0];

        var today = DateTime.Today;
        _fromDate = today;
        _toDate = today;
        _useDateFilter = false;
        UpdateFiltersSummary();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (Application.Current != null)
            Application.Current.RequestedThemeChanged += AppThemeChanged;

        BtnOpenKds.IsVisible = Perms.OrdersRead;

        if (!Perms.OrdersRead)
        {
            await DisplayAlert("Acceso restringido", "No tienes permiso para ver órdenes.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        ConfigureToolbar();

        await LoadOrdersAsync(!_initialized);
        _initialized = true;
    }

    protected override void OnDisappearing()
    {
        if (Application.Current != null)
            Application.Current.RequestedThemeChanged -= AppThemeChanged;

        base.OnDisappearing();
    }

    void ConfigureToolbar()
    {
        if (Perms.OrdersCreate)
        {
            if (_newOrderToolbar == null)
            {
                _newOrderToolbar = new ToolbarItem
                {
                    Text = "Nueva",
                    Order = ToolbarItemOrder.Primary,
                    Priority = 0
                };
                _newOrderToolbar.Clicked += NewOrderToolbar_Clicked;
                ToolbarItems.Add(_newOrderToolbar);
            }
            _newOrderToolbar.IsEnabled = true;
        }
        else if (_newOrderToolbar != null)
        {
            _newOrderToolbar.IsEnabled = false;
        }
    }

    async void NewOrderToolbar_Clicked(object? sender, EventArgs e)
    {
        await DisplayAlert("Órdenes", "Crear pedido aún no está disponible.", "OK");
    }

    async void OpenFilters_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_tablesCache.Count == 0)
                _tablesCache = await _ordersApi.ListTablesAsync(includeInactive: true);

            var popup = new OrderFiltersPopup(
                StatusOptions,
                _selectedStatusOption,
                ServiceTypeOptions,
                _selectedServiceOption,
                SourceOptions,
                _selectedSourceOption,
                _tablesCache,
                _useDateFilter,
                _fromDate,
                _toDate,
                _tableIdFilter);

            if (await this.ShowPopupAsync(popup) is OrderFiltersResult result)
            {
                ApplyFilters(result);
            }
        }
        catch (HttpRequestException ex)
        {
            var message = ErrorHandler.ObtenerMensajeHttp(new HttpResponseMessage(ex.StatusCode ?? HttpStatusCode.InternalServerError), ex.Message);
            await ErrorHandler.MostrarErrorUsuario(message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Órdenes – Cargar mesas para filtros");
        }
    }

    void ApplyFilters(OrderFiltersResult selection)
    {
        _selectedStatusOption = MatchOption(StatusOptions, selection.StatusOption) ?? StatusOptions.FirstOrDefault();
        _selectedServiceOption = MatchOption(ServiceTypeOptions, selection.ServiceOption) ?? ServiceTypeOptions.FirstOrDefault();
        _selectedSourceOption = MatchOption(SourceOptions, selection.SourceOption) ?? SourceOptions.FirstOrDefault();

        _useDateFilter = selection.UseDateFilter;
        _fromDate = selection.FromDate;
        _toDate = selection.ToDate;
        _tableIdFilter = selection.TableId;

        UpdateFiltersSummary();
        _ = LoadOrdersAsync(true);
    }

    async void OpenKds_Clicked(object sender, EventArgs e)
    {
        if (!Perms.OrdersRead)
        {
            await DisplayAlert("Acceso restringido", "No puedes ver el KDS.", "OK");
            return;
        }

        await Shell.Current.GoToAsync(nameof(KdsPage));
    }

    static FilterOption? MatchOption(IEnumerable<FilterOption> source, FilterOption? target)
        => target is null
            ? null
            : source.FirstOrDefault(o => string.Equals(o.Value, target.Value, StringComparison.Ordinal) && string.Equals(o.Label, target.Label, StringComparison.Ordinal));

    async Task LoadOrdersAsync(bool showSpinner)
    {
        if (!Perms.OrdersRead) return;

        try
        {
            UpdateFiltersSummary();
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                EmptyMessage = "Sin conexión a Internet.";
                _orders.Clear();
                await ErrorHandler.MostrarErrorUsuario("Sin conexión a Internet.");
                return;
            }

            if (showSpinner)
                IsRefreshing = true;

            var query = new OrderListQuery
            {
                statuses = _selectedStatusOption?.Value,
                serviceType = _selectedServiceOption?.Value,
                source = _selectedSourceOption?.Value,
                from = _useDateFilter ? _fromDate : null,
                to = _useDateFilter ? _toDate : null,
                tableId = _tableIdFilter,
                tzOffsetMinutes = OrdersApi.GetLocalTimezoneOffsetMinutes()
            };

            var list = await _ordersApi.ListAsync(query);

            _rawOrders.Clear();
            _rawOrders.AddRange(list);

            RebuildOrderListItems();
            EmptyMessage = _orders.Count == 0 ? "No se encontraron órdenes con esos filtros." : EmptyMessage;
        }
        catch (HttpRequestException ex)
        {
            var message = ErrorHandler.ObtenerMensajeHttp(new HttpResponseMessage(ex.StatusCode ?? HttpStatusCode.InternalServerError), ex.Message);
            await ErrorHandler.MostrarErrorUsuario(message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Órdenes – Cargar listado");
        }
        finally
        {
            if (showSpinner)
                IsRefreshing = false;
        }
    }

    void ApplySearchFilter()
    {
        var query = (_searchText ?? string.Empty).Trim().ToLowerInvariant();
        var source = string.IsNullOrEmpty(query)
            ? _all
            : _all.Where(o => o.Matches(query)).ToList();

        _orders.Clear();
        foreach (var item in source)
            _orders.Add(item);

        EmptyMessage = _orders.Count == 0 ? "No se encontraron órdenes con esos filtros." : "No hay órdenes";
    }

    void RebuildOrderListItems()
    {
        _all.Clear();
        foreach (var dto in _rawOrders)
            _all.Add(OrderListItem.From(dto));

        ApplySearchFilter();
    }

    void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue ?? string.Empty;
        ApplySearchFilter();
    }

    void SearchBox_SearchButtonPressed(object sender, EventArgs e)
    {
        _searchText = SearchBox.Text ?? string.Empty;
        ApplySearchFilter();
    }

    void AppThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        if (_rawOrders.Count == 0)
            return;

        if (Dispatcher.IsDispatchRequired)
        {
            Dispatcher.Dispatch(RebuildOrderListItems);
        }
        else
        {
            RebuildOrderListItems();
        }
    }

    void UpdateFiltersSummary()
    {
        var parts = new List<string>();
        if (_selectedStatusOption != null)
            parts.Add(_selectedStatusOption.Label);
        if (_selectedServiceOption != null && !string.IsNullOrWhiteSpace(_selectedServiceOption.Value))
            parts.Add(_selectedServiceOption.Label);
        if (_selectedSourceOption != null && !string.IsNullOrWhiteSpace(_selectedSourceOption.Value))
            parts.Add($"Canal {_selectedSourceOption.Label}");
        if (_tableIdFilter.HasValue)
        {
            var tableName = _tablesCache.FirstOrDefault(t => t.id == _tableIdFilter.Value)?.name;
            if (!string.IsNullOrWhiteSpace(tableName))
                parts.Add(tableName);
            else
                parts.Add($"Mesa {_tableIdFilter.Value}");
        }
        if (_useDateFilter && _fromDate.HasValue && _toDate.HasValue)
            parts.Add($"{_fromDate.Value:dd/MM} - {_toDate.Value:dd/MM}");

        FiltersSummary = parts.Count == 0 ? "Todos los pedidos" : string.Join(" • ", parts);
    }

    async void OrdersCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is not OrderListItem item)
            return;

        OrdersCollection.SelectedItem = null;

        await Shell.Current.GoToAsync($"{nameof(OrderDetailPage)}?orderId={item.Id}");
    }

    async void OrdersRefresh_Refreshing(object sender, EventArgs e)
    {
        await LoadOrdersAsync(false);
        IsRefreshing = false;
    }

    async void Retry_Clicked(object sender, EventArgs e)
    {
        IsRefreshing = true;
        await LoadOrdersAsync(true);
        IsRefreshing = false;
    }

    public class OrderListItem
    {
        public int Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string ServiceType { get; init; } = string.Empty;
        public string Source { get; init; } = string.Empty;
        public string TotalFormatted { get; init; } = string.Empty;
        public string Subtitle { get; init; } = string.Empty;
        public string PaymentsSummary { get; init; } = string.Empty;
        public string StatusBadge { get; init; } = string.Empty;
        public Color StatusColor { get; init; } = Colors.Gray;
        public Color CardBackgroundColor { get; init; } = Color.FromArgb("#FBF6F8");
        OrderSummaryDTO SourceDto { get; init; } = new();

        public bool Matches(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;

            return Code.Contains(query, StringComparison.OrdinalIgnoreCase)
                   || StatusBadge.Contains(query, StringComparison.OrdinalIgnoreCase)
                   || Subtitle.Contains(query, StringComparison.OrdinalIgnoreCase)
                   || Source.Contains(query, StringComparison.OrdinalIgnoreCase);
        }

        public static OrderListItem From(OrderSummaryDTO dto)
        {
            var totalFormatted = FormatCurrency(dto.totalCents);
            var paid = dto.paymentsTotalCents + dto.paymentsTipCents;
            var paymentsSummary = paid > 0
                ? $"Pagado {FormatCurrency(paid)} / {totalFormatted}"
                : "Sin pagos registrados";

            string statusLabel;
            Color statusColor;

            var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
            Color defaultCard = isDark ? Color.FromArgb("#1E1E1E") : Color.FromArgb("#FFFFFF");
            Color inProcessCard = isDark ? Color.FromArgb("#123C37") : Color.FromArgb("#E0F2F1");
            Color readyCard = isDark ? Color.FromArgb("#1B3C1B") : Color.FromArgb("#E8F5E9");
            Color servedCard = isDark ? Color.FromArgb("#4A3D00") : Color.FromArgb("#FFF9C4");
            Color holdCard = isDark ? Color.FromArgb("#4A3000") : Color.FromArgb("#FFF3E0");
            Color canceledCard = isDark ? Color.FromArgb("#442020") : Color.FromArgb("#FDE0DC");

            Color cardColor = defaultCard;
            if (dto.status == "OPEN")
            {
                if (dto.servedAt.HasValue)
                {
                    statusLabel = "Servido";
                    statusColor = Color.FromArgb("#FBC02D");
                    cardColor = servedCard;
                }
                else if (dto.readyAt.HasValue)
                {
                    statusLabel = "Listo";
                    statusColor = Color.FromArgb("#388E3C");
                    cardColor = readyCard;
                }
                else
                {
                    statusLabel = "En proceso";
                    statusColor = Color.FromArgb("#00796B");
                    cardColor = inProcessCard;
                }
            }
            else
            {
                statusLabel = dto.status switch
                {
                    "HOLD" => "En pausa",
                    "CLOSED" => "Cerrado",
                    "CANCELED" => "Cancelado",
                    "DRAFT" => "Borrador",
                    _ => dto.status
                };

                statusColor = dto.status switch
                {
                    "HOLD" => Color.FromArgb("#FFA000"),
                    "CLOSED" => Color.FromArgb("#33691E"),
                    "CANCELED" => Color.FromArgb("#D32F2F"),
                    "DRAFT" => Color.FromArgb("#546E7A"),
                    _ => Colors.Gray
                };

                cardColor = dto.status switch
                {
                    "HOLD" => holdCard,
                    "CANCELED" => canceledCard,
                    "CLOSED" => readyCard,
                    _ => defaultCard
                };
            }

            var serviceLabel = dto.serviceType switch
            {
                "DINE_IN" => "En mesa",
                "TAKEAWAY" => "Para llevar",
                "DELIVERY" => "Delivery",
                _ => dto.serviceType
            };

            var subtitleParts = new List<string> { serviceLabel };
            if (dto.table != null)
                subtitleParts.Add(dto.table.name);
            else if (dto.tableId.HasValue)
                subtitleParts.Add($"Mesa {dto.tableId.Value}");

            if (dto.platformMarkupPct.HasValue && dto.platformMarkupPct.Value != 0)
                subtitleParts.Add($"Markup {dto.platformMarkupPct.Value:0.##}%");

            if (dto.servedBy != null && !string.IsNullOrWhiteSpace(dto.servedBy.name))
                subtitleParts.Add($"Atiende {dto.servedBy.name}");

            if (dto.openedAt.HasValue)
                subtitleParts.Add(dto.openedAt.Value.ToLocalTime().ToString("HH:mm"));

            return new OrderListItem
            {
                Id = dto.id,
                Code = dto.code,
                Status = dto.status,
                ServiceType = dto.serviceType,
                Source = dto.source,
                TotalFormatted = totalFormatted,
                PaymentsSummary = paymentsSummary,
                Subtitle = string.Join(" • ", subtitleParts.Where(s => !string.IsNullOrWhiteSpace(s))),
                StatusBadge = statusLabel,
                StatusColor = statusColor,
                CardBackgroundColor = cardColor,
                SourceDto = dto
            };
        }

        static string FormatCurrency(int cents)
        {
            var value = cents / 100m;
            return value.ToString("C", CultureInfo.CurrentCulture);
        }
    }
}

public class FilterOption
{
    public FilterOption(string label, string? value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }
    public string? Value { get; }
    public override string ToString() => Label;
}

public record OrderFiltersResult(
    FilterOption? StatusOption,
    FilterOption? ServiceOption,
    FilterOption? SourceOption,
    bool UseDateFilter,
    DateTime? FromDate,
    DateTime? ToDate,
    int? TableId);
