using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using Imdeliceapp;
using Imdeliceapp.Helpers;
using Imdeliceapp.Pages;
using Imdeliceapp.Models;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices;

namespace Imdeliceapp.Popups;

public partial class CartReviewPopup : Popup
{
    readonly IList<TakeOrderPage.CartEntry> _source;
    readonly OrdersApi _ordersApi;
    readonly CartReviewViewModel _viewModel;

    public CartReviewPopup(IList<TakeOrderPage.CartEntry> source, TakeOrderPage.OrderHeaderState headerState, OrdersApi ordersApi)
    {
        InitializeComponent();
        _source = source;
        _ordersApi = ordersApi;
        _viewModel = new CartReviewViewModel(source, headerState, CloseWithResult);
        BindingContext = _viewModel;
        Opened += CartReviewPopup_Opened;

        var display = DeviceDisplay.MainDisplayInfo;
        var height = display.Height / display.Density;
        var width = 420;
        Size = new Size(width, height * 0.92);
    }

    async void CartReviewPopup_Opened(object? sender, EventArgs e)
    {
        Opened -= CartReviewPopup_Opened;
        var error = await _viewModel.InitializeAsync(_ordersApi);
        if (!string.IsNullOrWhiteSpace(error) && Application.Current?.MainPage != null)
            await Application.Current.MainPage.DisplayAlert("Órdenes", error, "OK");
    }

    void CloseButton_Clicked(object sender, EventArgs e)
        => CloseWithResult(new CartPopupResult(CartPopupAction.None));

    void RemoveButton_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not TakeOrderPage.CartEntry entry)
            return;

        if (_source.Contains(entry))
            _source.Remove(entry);

        _viewModel.Remove(entry);
    }

    void EditButton_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not TakeOrderPage.CartEntry entry)
            return;

        CloseWithResult(new CartPopupResult(CartPopupAction.EditLine, entry));
    }

    void IncreaseQuantity_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not TakeOrderPage.CartEntry entry)
            return;

        entry.Quantity += 1;
    }

    void DecreaseQuantity_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not TakeOrderPage.CartEntry entry)
            return;

        entry.Quantity = Math.Max(1, entry.Quantity - 1);
    }

    void CloseWithResult(CartPopupResult result) => Close(result);
}

class CartReviewViewModel : INotifyPropertyChanged
{
    readonly ObservableCollection<TakeOrderPage.CartEntry> _items;
    readonly Action<CartPopupResult> _close;
    readonly TakeOrderPage.OrderHeaderState _stateSnapshot;
    readonly int? _servedByUserId;
    readonly OptionVm _posSourceOption;
    readonly Dictionary<string, int> _channelMarkupDefaults = new(StringComparer.OrdinalIgnoreCase);

    bool _tablesLoaded;
    bool _channelConfigsLoaded;
    int? _initialTableId;

    string _markupText = string.Empty;
    bool _markupDirty;

    public string ServedByDisplay { get; }
    bool _isDetailsExpanded;


    public CartReviewViewModel(IList<TakeOrderPage.CartEntry> source, TakeOrderPage.OrderHeaderState? state, Action<CartPopupResult> close)
    {
        _close = close;
        _stateSnapshot = state?.Clone() ?? TakeOrderPage.OrderHeaderState.CreateDefault();

        _items = new ObservableCollection<TakeOrderPage.CartEntry>(source);
        _items.CollectionChanged += ItemsOnCollectionChanged;
        foreach (var entry in _items)
            entry.PropertyChanged += EntryOnPropertyChanged;

        ServiceTypeOptions = new List<OptionVm>
        {
            new("Servicio en mesa", "DINE_IN"),
            new("Para llevar", "TAKEAWAY"),
            new("Entrega a domicilio", "DELIVERY")
        };
        SourceOptions = new List<OptionVm>
        {
            new("POS / Caja", "POS"),
            new("Uber Eats", "UBER"),
            new("DiDi Food", "DIDI"),
            new("Rappi", "RAPPI")
        };
        _posSourceOption = FindByValue(SourceOptions, "POS") ?? SourceOptions.First();
        StatusOptions = new List<OptionVm>
        {
            new("Abierto", "OPEN"),
            new("En pausa", "HOLD"),
            new("Borrador", "DRAFT")
        };

        _markupText = _stateSnapshot.PlatformMarkupPct?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        _markupDirty = !string.IsNullOrWhiteSpace(_markupText);

        SelectedServiceType = FindByValue(ServiceTypeOptions, _stateSnapshot.ServiceType) ?? ServiceTypeOptions.First();
        SelectedSource = FindByValue(SourceOptions, _stateSnapshot.Source) ?? _posSourceOption;
        if (!IsSourceEnabled)
            ForceSourcePos();
        SelectedStatus = FindByValue(StatusOptions, _stateSnapshot.Status) ?? StatusOptions.First();

        CoversText = _stateSnapshot.Covers?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        Note = _stateSnapshot.Note ?? string.Empty;
        CustomerName = _stateSnapshot.CustomerName ?? string.Empty;
        CustomerPhone = _stateSnapshot.CustomerPhone ?? string.Empty;
        ExternalRef = _stateSnapshot.ExternalRef ?? string.Empty;
        DeliveryFeeText = _stateSnapshot.DeliveryFeeCents.HasValue
            ? _stateSnapshot.DeliveryFeeCents.Value.ToCurrency().ToString("0.00", CultureInfo.CurrentCulture)
            : string.Empty;
        PrepEtaText = _stateSnapshot.PrepEtaMinutes?.ToString(CultureInfo.InvariantCulture) ?? "30";
        _servedByUserId = ResolveServedByUserId(_stateSnapshot.ServedByUserId);
        _stateSnapshot.ServedByUserId = _servedByUserId;
        ServedByDisplay = BuildServedByDisplay(_servedByUserId);
        _initialTableId = _stateSnapshot.TableId;

        ContinueCommand = new Command(() => _close(new CartPopupResult(CartPopupAction.None)));
        CheckoutCommand = new Command(ExecuteCheckout, () => CanCheckout);
        ToggleDetailsCommand = new Command(() => IsDetailsExpanded = !IsDetailsExpanded);

        RefreshTotals();
        _isDetailsExpanded = Preferences.Default.Get("order_popup_expanded", true);
        OnPropertyChanged(nameof(IsDetailsExpanded));
        OnPropertyChanged(nameof(MarkupText));
        OnPropertyChanged(nameof(MarkupHint));
        OnPropertyChanged(nameof(IsMarkupValid));
    }

    public ObservableCollection<TakeOrderPage.CartEntry> Items => _items;

    public IReadOnlyList<OptionVm> ServiceTypeOptions { get; }
    public IReadOnlyList<OptionVm> SourceOptions { get; }
    public IReadOnlyList<OptionVm> StatusOptions { get; }

    OptionVm? _selectedServiceType;
    public OptionVm? SelectedServiceType
    {
        get => _selectedServiceType;
        set
        {
            if (_selectedServiceType == value) return;
            _selectedServiceType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowTableSelectors));
            OnPropertyChanged(nameof(ShowCustomerFields));
            OnPropertyChanged(nameof(ShowDeliveryFee));
            OnPropertyChanged(nameof(IsSourceEnabled));
            if (!IsSourceEnabled)
                ForceSourcePos();
            OnPropertyChanged(nameof(CanCheckout));
            OnPropertyChanged(nameof(MarkupHint));
            (CheckoutCommand as Command)?.ChangeCanExecute();
        }
    }

    OptionVm? _selectedSource;
    public OptionVm? SelectedSource
    {
        get => _selectedSource;
        set
        {
            var newValue = value ?? _posSourceOption;
            if (!IsSourceEnabled)
                newValue = _posSourceOption;
            var previous = _selectedSource;
            if (previous == newValue) return;
            _selectedSource = newValue;
            if (previous != null &&
                !string.Equals(previous.Value, newValue.Value, StringComparison.OrdinalIgnoreCase))
                _markupDirty = false;

            if (_channelConfigsLoaded)
            {
                if (!_markupDirty)
                    ApplyDefaultMarkupForSource();
                else
                    OnPropertyChanged(nameof(MarkupHint));
            }
            else if (_markupDirty)
            {
                OnPropertyChanged(nameof(MarkupHint));
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCheckout));
            OnPropertyChanged(nameof(MarkupHint));
            (CheckoutCommand as Command)?.ChangeCanExecute();
        }
    }

    OptionVm? _selectedStatus;
    public OptionVm? SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (_selectedStatus == value) return;
            _selectedStatus = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCheckout));
            (CheckoutCommand as Command)?.ChangeCanExecute();
        }
    }

    public string MarkupText
    {
        get => _markupText;
        set
        {
            var normalized = (value ?? string.Empty).Trim();
            if (_markupText == normalized) return;
            _markupText = normalized;
            _markupDirty = true;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MarkupHint));
            OnPropertyChanged(nameof(IsMarkupValid));
            OnPropertyChanged(nameof(CanCheckout));
            (CheckoutCommand as Command)?.ChangeCanExecute();
        }
    }

    public string MarkupHint
    {
        get
        {
            if (!IsMarkupValid)
                return "Ingresa un número entero o deja vacío para usar el valor por defecto.";

            if (!_channelConfigsLoaded)
                return "Cargando recargo del canal…";

            var sourceDisplay = SelectedSource?.Display ?? "Canal";
            if (ParsedMarkup.HasValue)
                return $"{sourceDisplay}: {ParsedMarkup.Value}% aplicado";

            var sourceValue = SelectedSource?.Value;
            if (!string.IsNullOrWhiteSpace(sourceValue) &&
                _channelMarkupDefaults.TryGetValue(sourceValue, out var pct))
            {
                if (pct > 0)
                    return $"Se aplicará {pct}% por defecto para {sourceDisplay}. Deja vacío para usarlo.";
                return $"Sin recargo configurado para {sourceDisplay}.";
            }

            return "Sin recargo configurado.";
        }
    }

    public bool IsMarkupValid => string.IsNullOrWhiteSpace(_markupText) || ParsedMarkup.HasValue;

    public bool ShowMarkupField => true;

    string _coversText = string.Empty;
    public string CoversText
    {
        get => _coversText;
        set
        {
            if (_coversText == value) return;
            _coversText = value;
            OnPropertyChanged();
        }
    }

    string _prepEtaText = string.Empty;
    public string PrepEtaText
    {
        get => _prepEtaText;
        set
        {
            if (_prepEtaText == value) return;
            _prepEtaText = value;
            OnPropertyChanged();
        }
    }

    string _note = string.Empty;
    public string Note
    {
        get => _note;
        set
        {
            if (_note == value) return;
            _note = value;
            OnPropertyChanged();
        }
    }

    string _customerName = string.Empty;
    public string CustomerName
    {
        get => _customerName;
        set
        {
            if (_customerName == value) return;
            _customerName = value;
            OnPropertyChanged();
        }
    }

    string _customerPhone = string.Empty;
    public string CustomerPhone
    {
        get => _customerPhone;
        set
        {
            if (_customerPhone == value) return;
            _customerPhone = value;
            OnPropertyChanged();
        }
    }

    string _externalRef = string.Empty;
    public string ExternalRef
    {
        get => _externalRef;
        set
        {
            if (_externalRef == value) return;
            _externalRef = value;
            OnPropertyChanged();
        }
    }

    string _deliveryFeeText = string.Empty;
    public string DeliveryFeeText
    {
        get => _deliveryFeeText;
        set
        {
            if (_deliveryFeeText == value) return;
            _deliveryFeeText = value;
            OnPropertyChanged();
            RefreshTotals();
        }
    }

    public ObservableCollection<TableOptionVm> TableOptions { get; } = new();

    TableOptionVm? _selectedTable;
    public TableOptionVm? SelectedTable
    {
        get => _selectedTable;
        set
        {
            if (_selectedTable == value) return;
            _selectedTable = value;
            OnPropertyChanged();
        }
    }

    bool _isLoadingTables;
    public bool IsLoadingTables
    {
        get => _isLoadingTables;
        private set
        {
            if (_isLoadingTables == value) return;
            _isLoadingTables = value;
            OnPropertyChanged();
        }
    }

    public bool ShowTableSelectors => string.Equals(SelectedServiceType?.Value, "DINE_IN", StringComparison.OrdinalIgnoreCase);
    public bool ShowCustomerFields => !string.Equals(SelectedServiceType?.Value, "DINE_IN", StringComparison.OrdinalIgnoreCase);
    public bool ShowDeliveryFee => string.Equals(SelectedServiceType?.Value, "DELIVERY", StringComparison.OrdinalIgnoreCase);
    public bool IsSourceEnabled => string.Equals(SelectedServiceType?.Value, "DELIVERY", StringComparison.OrdinalIgnoreCase);

    decimal _total;
    public decimal Total
    {
        get => _total;
        private set
        {
            if (_total == value) return;
            _total = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TotalFormatted));
        }
    }

    public string TotalFormatted => Total.ToString("$0.00", CultureInfo.CurrentCulture);
    public bool HasItems => _items.Count > 0;

    public bool CanCheckout =>
        !string.IsNullOrWhiteSpace(SelectedServiceType?.Value) &&
        !string.IsNullOrWhiteSpace(SelectedSource?.Value) &&
        !string.IsNullOrWhiteSpace(SelectedStatus?.Value) &&
        IsMarkupValid;

    public ICommand ContinueCommand { get; }
    public ICommand CheckoutCommand { get; }
    public ICommand ToggleDetailsCommand { get; }

    public bool IsDetailsExpanded
    {
        get => _isDetailsExpanded;
        set
        {
            if (_isDetailsExpanded == value) return;
            _isDetailsExpanded = value;
            OnPropertyChanged();
        }
    }

    public async Task<string?> InitializeAsync(OrdersApi ordersApi)
    {
        if (ordersApi == null)
            return null;

        var requestedTables = false;
        try
        {
            if (!_channelConfigsLoaded)
            {
                var configs = await ordersApi.ListChannelConfigsAsync();
                _channelMarkupDefaults.Clear();
                foreach (var cfg in configs)
                    _channelMarkupDefaults[cfg.source] = cfg.markupPct;
                _channelConfigsLoaded = true;

                if (!_markupDirty)
                    ApplyDefaultMarkupForSource();
                else
                    OnPropertyChanged(nameof(MarkupHint));
            }

            if (!_tablesLoaded)
            {
                requestedTables = true;
                IsLoadingTables = true;
                var tables = await ordersApi.ListTablesAsync(includeInactive: true);
                TableOptions.Clear();
                foreach (var dto in tables
                         .OrderByDescending(t => t.isActive)
                         .ThenBy(t => t.name ?? $"Mesa {t.id}"))
                    TableOptions.Add(new TableOptionVm(dto));

                _tablesLoaded = true;

                if (_initialTableId.HasValue)
                    SelectedTable = TableOptions.FirstOrDefault(t => t.Id == _initialTableId.Value);
            }

            return null;
        }
        catch (HttpRequestException ex)
        {
            var message = ErrorHandler.ObtenerMensajeHttp(
                new HttpResponseMessage(ex.StatusCode ?? HttpStatusCode.InternalServerError), ex.Message);
            return message;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        finally
        {
            if (requestedTables)
                IsLoadingTables = false;
        }
    }

    public void Remove(TakeOrderPage.CartEntry entry)
    {
        if (_items.Contains(entry))
        {
            entry.PropertyChanged -= EntryOnPropertyChanged;
            _items.Remove(entry);
        }

        RefreshTotals();
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(CanCheckout));
        (CheckoutCommand as Command)?.ChangeCanExecute();
    }

    void ExecuteCheckout()
    {
        var header = BuildOrderHeaderState();
        Preferences.Default.Set("order_popup_expanded", IsDetailsExpanded);
        _close(new CartPopupResult(CartPopupAction.Checkout, null, header));
    }

    void ItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (TakeOrderPage.CartEntry entry in e.NewItems)
                entry.PropertyChanged += EntryOnPropertyChanged;
        }

        if (e.OldItems != null)
        {
            foreach (TakeOrderPage.CartEntry entry in e.OldItems)
                entry.PropertyChanged -= EntryOnPropertyChanged;
        }

        RefreshTotals();
        OnPropertyChanged(nameof(HasItems));
    }

    void EntryOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TakeOrderPage.CartEntry.LineTotal) or nameof(TakeOrderPage.CartEntry.Quantity))
            RefreshTotals();
    }

    void RefreshTotals()
    {
        var itemsTotal = _items.Sum(i => i.LineTotal);
        var deliveryFee = ParseCurrencyAmount(DeliveryFeeText) ?? 0m;
        Total = itemsTotal + deliveryFee;
    }

    void ForceSourcePos()
    {
        if (_selectedSource == _posSourceOption)
            return;
        _selectedSource = _posSourceOption;
        _markupDirty = false;
        if (_channelConfigsLoaded)
            ApplyDefaultMarkupForSource();
        else
            OnPropertyChanged(nameof(MarkupHint));
        OnPropertyChanged(nameof(SelectedSource));
        OnPropertyChanged(nameof(CanCheckout));
        OnPropertyChanged(nameof(MarkupHint));
        (CheckoutCommand as Command)?.ChangeCanExecute();
    }

    TakeOrderPage.OrderHeaderState BuildOrderHeaderState()
    {
        var state = _stateSnapshot.Clone();
        state.ServiceType = SelectedServiceType?.Value ?? _stateSnapshot.ServiceType;
        state.Source = SelectedSource?.Value ?? _stateSnapshot.Source;
        state.Status = SelectedStatus?.Value ?? _stateSnapshot.Status;

        state.TableId = SelectedTable?.Id;
        state.TableName = SelectedTable?.Name;
        state.Covers = ParseNullableInt(CoversText);
        state.PrepEtaMinutes = ParseNullableInt(PrepEtaText);
        state.ServedByUserId = _servedByUserId;

        state.Note = Normalize(Note);
        state.CustomerName = Normalize(CustomerName);
        state.CustomerPhone = Normalize(CustomerPhone);
        state.ExternalRef = Normalize(ExternalRef);
        state.PlatformMarkupPct = ParseMarkupValue();
        state.DeliveryFeeCents = ParseCurrencyCents(DeliveryFeeText);

        return state;
    }

    static int? ParseCurrencyCents(string? text)
    {
        var amount = ParseCurrencyAmount(text);
        if (!amount.HasValue)
            return null;
        return (int)Math.Round(amount.Value * 100m, MidpointRounding.AwayFromZero);
    }

    static decimal? ParseCurrencyAmount(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var normalized = text.Trim();
        if (decimal.TryParse(normalized, NumberStyles.Currency, CultureInfo.CurrentCulture, out var parsed))
            return parsed;
        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
            return parsed;

        return null;
    }

    decimal? ParsedMarkup => ParseMarkupValue();

    void ApplyDefaultMarkupForSource()
    {
        if (!_channelConfigsLoaded)
        {
            OnPropertyChanged(nameof(MarkupHint));
            return;
        }

        var sourceValue = SelectedSource?.Value;
        var normalized = string.Empty;
        if (!string.IsNullOrWhiteSpace(sourceValue) && _channelMarkupDefaults.TryGetValue(sourceValue, out var pct))
            normalized = pct > 0 ? pct.ToString(CultureInfo.InvariantCulture) : string.Empty;

        SetMarkupFromSystem(normalized);
        _markupDirty = false;
    }

    void SetMarkupFromSystem(string value)
    {
        var normalized = value ?? string.Empty;
        var changed = _markupText != normalized;
        _markupText = normalized;
        if (changed)
            OnPropertyChanged(nameof(MarkupText));
        OnPropertyChanged(nameof(IsMarkupValid));
        OnPropertyChanged(nameof(MarkupHint));
        OnPropertyChanged(nameof(CanCheckout));
        (CheckoutCommand as Command)?.ChangeCanExecute();
    }

    static int? ResolveServedByUserId(int? existing)
    {
        if (existing.HasValue && existing.Value > 0)
            return existing;

        var userId = Preferences.Default.Get("user_id", 0);
        return userId > 0 ? userId : null;
    }

    static string BuildServedByDisplay(int? userId)
    {
        var name = Preferences.Default.Get("usuario_nombre", string.Empty);
        if (userId.HasValue)
        {
            if (!string.IsNullOrWhiteSpace(name))
                return $"{name}";
            return $"ID {userId.Value}";
        }

        return string.IsNullOrWhiteSpace(name) ? "Sin asignar" : name;
    }

    static int? ParseNullableInt(string? value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        return null;
    }

    decimal? ParseMarkupValue()
    {
        if (string.IsNullOrWhiteSpace(_markupText))
            return null;
        if (decimal.TryParse(_markupText, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        return null;
    }

    static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public class TableOptionVm
    {
        public TableOptionVm(TableDTO dto)
        {
            Id = dto.id;
            Name = dto.name ?? $"Mesa {dto.id}";
            Seats = dto.seats;
            IsActive = dto.isActive;
        }

        public int Id { get; }
        public string Name { get; }
        public int? Seats { get; }
        public bool IsActive { get; }

        public string DisplayName
        {
            get
            {
                var seatsText = Seats.HasValue ? $" · {Seats.Value} lugares" : string.Empty;
                var status = IsActive ? string.Empty : " (inactiva)";
                return $"{Name}{seatsText}{status}";
            }
        }
    }

    public class OptionVm
    {
        public OptionVm(string display, string value)
        {
            Display = display;
            Value = value;
        }

        public string Display { get; }
        public string Value { get; }

        public override string ToString() => Display;
    }

    static OptionVm? FindByValue(IEnumerable<OptionVm> source, string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : source.FirstOrDefault(o => string.Equals(o.Value, value, StringComparison.OrdinalIgnoreCase));
}
