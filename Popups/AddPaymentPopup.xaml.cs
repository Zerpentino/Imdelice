using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Views;
using Imdeliceapp.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Windows.Input;

namespace Imdeliceapp.Popups;

public partial class AddPaymentPopup : Popup
{
    readonly ViewModel _viewModel;

    public AddPaymentPopup(int outstandingCents)
    {
        InitializeComponent();
        _viewModel = new ViewModel(outstandingCents);
        BindingContext = _viewModel;
    }

    public record Result(AddPaymentDto Payload);

    void Cancel_Clicked(object sender, EventArgs e) => Close(null);

    void Save_Clicked(object sender, EventArgs e)
    {
        if (!_viewModel.TryBuildDto(out var dto, out var message))
        {
            _viewModel.ValidationMessage = message;
            return;
        }

        _viewModel.ValidationMessage = null;
        Close(new Result(dto));
    }

    class ViewModel : INotifyPropertyChanged
    {
        readonly int _outstandingCents;
        int _amountCents;
        string _receivedText = string.Empty;
        bool _isEditingAmount;
        bool _isConfirming;

        public ViewModel(int outstandingCents)
        {
            _outstandingCents = outstandingCents;
            _amountCents = Math.Max(outstandingCents, 0);

            Methods = new ObservableCollection<PaymentMethodOption>(CreateMethods());
            SuggestedAmounts = new ObservableCollection<SuggestedAmountOption>();

            SelectMethodCommand = new Command<PaymentMethodOption?>(SelectMethod);
            FillExactCommand = new Command(SetExactReceived);
            ApplySuggestedAmountCommand = new Command<int>(ApplySuggestedAmount);
            BeginAmountEditCommand = new Command(BeginAmountEdit);
            CancelAmountEditCommand = new Command(CancelAmountEdit);
            SaveAmountEditCommand = new Command(SaveAmountEdit);
            ContinueCommand = new Command(EnterConfirmation);
            BackFromConfirmationCommand = new Command(LeaveConfirmation);

            SelectedMethod = Methods.FirstOrDefault();
            AmountEditText = FormatPlain(_amountCents);
            ReceivedText = FormatPlain(_amountCents);
            RefreshSuggestedAmounts();
        }

        public ObservableCollection<PaymentMethodOption> Methods { get; }
        public ObservableCollection<SuggestedAmountOption> SuggestedAmounts { get; }
        public ICommand SelectMethodCommand { get; }
        public ICommand FillExactCommand { get; }
        public ICommand ApplySuggestedAmountCommand { get; }
        public ICommand BeginAmountEditCommand { get; }
        public ICommand CancelAmountEditCommand { get; }
        public ICommand SaveAmountEditCommand { get; }
        public ICommand ContinueCommand { get; }
        public ICommand BackFromConfirmationCommand { get; }

        PaymentMethodOption? _selectedMethod;
        public PaymentMethodOption? SelectedMethod
        {
            get => _selectedMethod;
            set
            {
                if (_selectedMethod == value) return;
                _selectedMethod = value;
                UpdateSelectedFlags();
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedMethodDisplay));
                NotifyCashFieldChanges();
                EnsureDefaultReceivedForMethod();
                ResetConfirmation();
            }
        }

        public string SelectedMethodDisplay => SelectedMethod?.Display ?? "Selecciona un método";

        void SelectMethod(PaymentMethodOption? option)
        {
            if (option == null)
                return;
            SelectedMethod = option;
        }

        void UpdateSelectedFlags()
        {
            if (Methods == null) return;
            foreach (var method in Methods)
                method.IsSelected = method == _selectedMethod;
        }

        public string AmountDisplay => FormatCurrency(_amountCents);

        public string OutstandingDisplay => FormatCurrency(_outstandingCents);
        string _amountEditText = string.Empty;
        public string AmountEditText
        {
            get => _amountEditText;
            set
            {
                if (_amountEditText == value) return;
                _amountEditText = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public bool IsEditingAmount
        {
            get => _isEditingAmount;
            private set
            {
                if (_isEditingAmount == value) return;
                _isEditingAmount = value;
                OnPropertyChanged();
            }
        }

        public bool IsConfirming
        {
            get => _isConfirming;
            private set
            {
                if (_isConfirming == value) return;
                _isConfirming = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEditingStage));
            }
        }

        public bool IsEditingStage => !IsConfirming;

        void BeginAmountEdit()
        {
            if (IsEditingAmount) return;
            AmountEditText = FormatPlain(_amountCents);
            ValidationMessage = null;
            IsEditingAmount = true;
        }

        void CancelAmountEdit()
        {
            AmountEditText = FormatPlain(_amountCents);
            IsEditingAmount = false;
        }

        void SaveAmountEdit()
        {
            if (!TryParseMoney(AmountEditText, required: true, out var cents, out var message))
            {
                ValidationMessage = message;
                return;
            }

            if (cents <= 0)
            {
                ValidationMessage = "El monto debe ser mayor a 0.";
                return;
            }

            ValidationMessage = null;
            var previousAmount = _amountCents;
            _amountCents = cents;
            OnPropertyChanged(nameof(AmountDisplay));
            AmountEditText = FormatPlain(_amountCents);
            RefreshSuggestedAmounts();
            var received = ParseMoneyLoose(_receivedText);
            if (!ShowCashFields || (received.HasValue && received.Value == previousAmount))
                ReceivedText = FormatPlain(_amountCents);
            else
                UpdateCashFeedback();

            EnsureDefaultReceivedForMethod();
            ResetConfirmation();
            IsEditingAmount = false;
        }

        void EnsureDefaultReceivedForMethod()
        {
            if (ShowCashFields)
            {
                if (string.IsNullOrWhiteSpace(_receivedText))
                    ReceivedText = FormatPlain(_amountCents);
                return;
            }

            ReceivedText = FormatPlain(_amountCents);
        }

        public string ReceivedTitle => ShowCashFields ? "Efectivo recibido" : "Monto registrado";

        public string ReceivedText
        {
            get => _receivedText;
            set
            {
                var normalized = value ?? string.Empty;
                if (_receivedText == normalized) return;
                _receivedText = normalized;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ReceivedDisplay));
                OnPropertyChanged(nameof(ChangeDisplay));
                UpdateCashFeedback();
                ResetConfirmation();
            }
        }

        public string ReceivedDisplay
        {
            get
            {
                var cents = ParseMoneyLoose(_receivedText) ?? 0;
                return FormatCurrency(cents);
            }
        }

        public string ChangeDisplay
        {
            get
            {
                if (!ShowCashFields)
                    return "—";

                var received = ParseMoneyLoose(_receivedText);
                if (!received.HasValue || received.Value < _amountCents)
                    return "—";
                var change = received.Value - _amountCents;
                return change > 0 ? FormatCurrency(change) : "Exacto";
            }
        }

        public bool ShowCashFields => string.Equals(SelectedMethod?.Code, "CASH", StringComparison.OrdinalIgnoreCase);

        public string CashStatusMessage
        {
            get
            {
                if (!ShowCashFields)
                    return "Este método no requiere efectivo.";

                var received = ParseMoneyLoose(_receivedText);
                if (!received.HasValue)
                    return "Ingresa el efectivo recibido.";
                if (received.Value < _amountCents)
                {
                    var diff = _amountCents - received.Value;
                    return $"Faltan {FormatCurrency(diff)} por cobrar.";
                }

                var change = received.Value - _amountCents;
                return change > 0
                    ? $"Cambio: {FormatCurrency(change)}"
                    : "Exacto, sin cambio.";
            }
        }

        public bool HasCashStatusMessage => !string.IsNullOrWhiteSpace(CashStatusMessage);

        public Color CashStatusColor
        {
            get
            {
                var received = ParseMoneyLoose(_receivedText);
                var warning = ShowCashFields && (!received.HasValue || received.Value < _amountCents);
                return warning
                    ? Color.FromArgb("#D84315")
                    : Color.FromArgb("#556070");
            }
        }

        void UpdateCashFeedback()
        {
            OnPropertyChanged(nameof(CashStatusMessage));
            OnPropertyChanged(nameof(HasCashStatusMessage));
            OnPropertyChanged(nameof(CashStatusColor));
        }

        void SetExactReceived()
        {
            if (!ShowCashFields)
                return;
            ReceivedText = FormatPlain(_amountCents);
        }

        void ApplySuggestedAmount(int amountCents)
        {
            if (!ShowCashFields)
                return;
            ReceivedText = FormatPlain(amountCents);
        }

        void RefreshSuggestedAmounts()
        {
            SuggestedAmounts.Clear();
            var presets = new[] { 10000, 20000, 30000, 40000, 50000 };
            foreach (var amount in presets)
                SuggestedAmounts.Add(new SuggestedAmountOption(amount, FormatCurrency(amount)));
        }

        void EnterConfirmation()
        {
            if (!TryBuildDto(out _, out var message))
            {
                ValidationMessage = message;
                return;
            }

            ValidationMessage = null;
            IsConfirming = true;
        }

        void LeaveConfirmation()
        {
            IsConfirming = false;
        }

        void ResetConfirmation()
        {
            if (IsConfirming)
                IsConfirming = false;
        }

        string _tipText = "0";
        public string TipText
        {
            get => _tipText;
            set
            {
                if (_tipText == value) return;
                _tipText = value ?? "0";
                OnPropertyChanged();
                OnPropertyChanged(nameof(TipSummary));
                ResetConfirmation();
            }
        }

        string? _note;
        public string? Note
        {
            get => _note;
            set
            {
                if (_note == value) return;
                _note = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NoteSummary));
                ResetConfirmation();
            }
        }

        public string TipSummary
        {
            get
            {
                var tip = ParseMoneyLoose(TipText);
                return tip.HasValue && tip.Value > 0
                    ? FormatCurrency(tip.Value)
                    : "Sin propina";
            }
        }

        public string NoteSummary => string.IsNullOrWhiteSpace(Note) ? "Sin nota" : Note.Trim();

        public bool HasOutstandingHint => _outstandingCents > 0;

        public string OutstandingHint
        {
            get
            {
                if (_outstandingCents <= 0)
                    return string.Empty;
                return $"Pendiente por cobrar: {FormatCurrency(_outstandingCents)}";
            }
        }

        string? _validationMessage;
        public string? ValidationMessage
        {
            get => _validationMessage;
            set
            {
                if (_validationMessage == value) return;
                _validationMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasValidationMessage));
            }
        }

        public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

        public bool TryBuildDto(out AddPaymentDto? dto, out string? message)
        {
            dto = null;
            message = null;

            if (SelectedMethod == null)
            {
                message = "Selecciona un método de pago.";
                return false;
            }

            if (_amountCents <= 0)
            {
                message = "Define un monto mayor a 0.";
                return false;
            }

            if (!TryParseMoney(TipText, required: false, out var tipCents, out message))
                return false;

            int? receivedCents = null;
            int? changeCents = null;
            if (ShowCashFields)
            {
                if (!TryParseMoney(_receivedText, required: true, out var received, out message))
                    return false;
                if (received < _amountCents)
                {
                    message = "El efectivo recibido debe ser mayor o igual al monto.";
                    return false;
                }

                receivedCents = received;
                changeCents = received - _amountCents;
            }

            dto = new AddPaymentDto
            {
                method = SelectedMethod.Code,
                amountCents = _amountCents,
                tipCents = tipCents,
                note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim(),
                receivedAmountCents = receivedCents,
                changeCents = changeCents
            };
            return true;
        }

        void NotifyCashFieldChanges()
        {
            OnPropertyChanged(nameof(ShowCashFields));
            OnPropertyChanged(nameof(ReceivedTitle));
            OnPropertyChanged(nameof(ChangeDisplay));
            UpdateCashFeedback();
        }

        static string FormatPlain(int cents)
        {
            if (cents <= 0) return string.Empty;
            return (cents / 100m).ToString("0.##", CultureInfo.CurrentCulture);
        }

        static string FormatCurrency(int? cents)
        {
            var value = cents ?? 0;
            return (value / 100m).ToString("C", CultureInfo.CurrentCulture);
        }

        static int? ParseMoneyLoose(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;
            if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) || value < 0)
                return null;
            return (int)Math.Round(value * 100m, MidpointRounding.AwayFromZero);
        }

        static bool TryParseMoney(string? text, bool required, out int cents, out string? message)
        {
            cents = 0;
            message = null;
            var normalized = text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(normalized))
            {
                if (required)
                {
                    message = "Ingresa un monto válido.";
                    return false;
                }
                return true;
            }

            if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) || value < 0)
            {
                message = "Formato de monto inválido.";
                return false;
            }

            cents = (int)Math.Round(value * 100m, MidpointRounding.AwayFromZero);
            return true;
        }

        static IEnumerable<PaymentMethodOption> CreateMethods() => new[]
        {
            new PaymentMethodOption("CASH", "Efectivo"),
            new PaymentMethodOption("CARD", "Tarjeta"),
            new PaymentMethodOption("TRANSFER", "Transferencia"),
            new PaymentMethodOption("OTHER", "Otro")
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class PaymentMethodOption : INotifyPropertyChanged
    {
        public PaymentMethodOption(string code, string display)
        {
            Code = code;
            Display = display;
        }

        public string Code { get; }
        public string Display { get; }

        bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class SuggestedAmountOption
    {
        public SuggestedAmountOption(int amountCents, string label)
        {
            AmountCents = amountCents;
            Label = label;
        }

        public int AmountCents { get; }
        public string Label { get; }
    }
}
