using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Views;
using Imdeliceapp.Models;
using Microsoft.Maui.Controls;

namespace Imdeliceapp.Popups;

public partial class OrderItemEditorPopup : Popup
{
    readonly ViewModel _viewModel;

    public OrderItemEditorPopup(OrderItemDTO item, IReadOnlyList<ModifierOptionChoice> availableOptions, bool lockQuantity)
    {
        InitializeComponent();
        _viewModel = new ViewModel(item, availableOptions, lockQuantity);
        BindingContext = _viewModel;
    }

    public record Result(UpdateOrderItemDto? Payload);
    public record ModifierOptionChoice(int OptionId, string Display);

    void AddModifier_Clicked(object sender, EventArgs e) => _viewModel.AddModifier();

    void RemoveModifier_Clicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is ModifierEntryVm entry)
            _viewModel.RemoveModifier(entry);
    }

    void ClearModifiers_Clicked(object sender, EventArgs e) => _viewModel.ClearModifiers();

    void ClearNote_Clicked(object sender, EventArgs e) => _viewModel.ClearNote();

    void Cancel_Clicked(object sender, EventArgs e) => Close(null);

    void Save_Clicked(object sender, EventArgs e)
    {
        if (!_viewModel.TryBuildPayload(out var dto, out var message))
        {
            _viewModel.ValidationMessage = message;
            return;
        }

        _viewModel.ValidationMessage = null;
        Close(new Result(dto));
    }

    class ViewModel : INotifyPropertyChanged
    {
        readonly OrderItemDTO _item;
        readonly int _originalQuantity;
        readonly string? _originalNotes;
        readonly List<(int optionId, int quantity)> _originalModifiers;
        readonly IReadOnlyList<ModifierOptionChoice> _availableOptions;
        readonly bool _lockQuantity;

        public ViewModel(OrderItemDTO item, IReadOnlyList<ModifierOptionChoice> availableOptions, bool lockQuantity)
        {
            _item = item;
            _originalQuantity = item.quantity;
            _originalNotes = item.notes;
            _originalModifiers = item.modifiers?
                .Select(m => (m.optionId, m.quantity))
                .ToList() ?? new List<(int optionId, int quantity)>();
            _availableOptions = availableOptions ?? Array.Empty<ModifierOptionChoice>();
            _lockQuantity = lockQuantity;

            HeaderTitle = item.nameSnapshot ?? item.product?.name ?? $"Producto {item.productId}";
            Subtitle = $"{item.quantity}× · Estado: {TranslateStatus(item.status)}";
            QuantityValue = Math.Max(1, item.quantity);
            Notes = item.notes ?? string.Empty;
            ModifierEntries = new ObservableCollection<ModifierEntryVm>(
                item.modifiers?.Select(m => new ModifierEntryVm(m.optionId, m.quantity, m.nameSnapshot, _availableOptions)).ToList()
                ?? new List<ModifierEntryVm>());
        }

        public string HeaderTitle { get; }
        public string Subtitle { get; }

        double _quantityValue;
        public double QuantityValue
        {
            get => _quantityValue;
            set
            {
                var newValue = Math.Max(1, Math.Round(value));
                if (Math.Abs(_quantityValue - newValue) < double.Epsilon) return;
                _quantityValue = newValue;
                OnPropertyChanged();
                OnPropertyChanged(nameof(QuantityDisplay));
            }
        }

        public string QuantityDisplay => $"{(int)Math.Round(QuantityValue)}×";
        public bool IsQuantityEditable => !_lockQuantity;
        public bool IsQuantityLocked => _lockQuantity;
        public string QuantityLockedLabel => _lockQuantity ? $"Cantidad fija: {QuantityDisplay}" : string.Empty;

        string _notes = string.Empty;
        public string Notes
        {
            get => _notes;
            set
            {
                if (_notes == value) return;
                _notes = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string NoteHint => string.IsNullOrWhiteSpace(_originalNotes)
            ? "Actualmente sin nota"
            : $"Original: {_originalNotes}";

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

        public ObservableCollection<ModifierEntryVm> ModifierEntries { get; }

        public bool HasPredefinedOptions => _availableOptions.Count > 0;

        public void AddModifier()
        {
            ModifierEntries.Add(new ModifierEntryVm(0, 1, null, _availableOptions));
        }

        public void RemoveModifier(ModifierEntryVm entry)
        {
            ModifierEntries.Remove(entry);
        }

        public void ClearModifiers()
        {
            ModifierEntries.Clear();
        }

        public void ClearNote()
        {
            Notes = string.Empty;
        }

        public bool TryBuildPayload(out UpdateOrderItemDto? dto, out string? message)
        {
            dto = new UpdateOrderItemDto();
            message = null;
            var hasChanges = false;

            if (!_lockQuantity)
            {
                var newQuantity = (int)Math.Round(QuantityValue);
                if (newQuantity != _originalQuantity)
                {
                    dto.quantity = newQuantity;
                    hasChanges = true;
                }
            }

            var normalizedNotes = string.IsNullOrWhiteSpace(Notes) ? string.Empty : Notes.Trim();
            var originalNormalized = string.IsNullOrWhiteSpace(_originalNotes) ? string.Empty : _originalNotes.Trim();
            if (!string.Equals(normalizedNotes, originalNormalized, StringComparison.Ordinal))
            {
                dto.notes = normalizedNotes;
                hasChanges = true;
            }

            if (!TryBuildModifierSelections(out var selections, out message))
            {
                dto = null;
                return false;
            }

            if (selections != null)
            {
                if (ModifiersChanged(selections))
                {
                    dto.replaceModifiers = selections;
                    hasChanges = true;
                }
            }

            if (!hasChanges)
            {
                dto = null;
                message = "No hay cambios por guardar.";
                return false;
            }

            return true;
        }

        bool TryBuildModifierSelections(out List<OrderModifierSelectionInput>? selections, out string? message)
        {
            message = null;
            selections = null;
            if (ModifierEntries.Count == 0)
            {
                selections = new List<OrderModifierSelectionInput>();
                return true;
            }

            var list = new List<OrderModifierSelectionInput>();
            foreach (var entry in ModifierEntries)
            {
                if (!entry.TryGetOptionId(out var optionId))
                {
                    message = "Selecciona un extra válido o ingresa el ID manual.";
                    return false;
                }

                if (!entry.TryGetQuantity(out var qty))
                {
                    message = "Verifica las cantidades de los extras.";
                    return false;
                }

                list.Add(new OrderModifierSelectionInput
                {
                    optionId = optionId,
                    quantity = qty
                });
            }

            selections = list;
            return true;
        }

        bool ModifiersChanged(IReadOnlyList<OrderModifierSelectionInput> newList)
        {
            if (newList.Count != _originalModifiers.Count)
                return true;

            for (var i = 0; i < newList.Count; i++)
            {
                var reference = _originalModifiers[i];
                var candidate = newList[i];
                var candidateQty = candidate.quantity.GetValueOrDefault(1);
                if (candidate.optionId != reference.optionId || candidateQty != reference.quantity)
                    return true;
            }

            return false;
        }

        static string TranslateStatus(string status) => status?.ToUpperInvariant() switch
        {
            "NEW" => "Nuevo",
            "IN_PROGRESS" => "Preparando",
            "READY" => "Listo",
            "SERVED" => "Servido",
            "CANCELED" => "Cancelado",
            _ => status ?? string.Empty
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class ModifierEntryVm : INotifyPropertyChanged
    {
        readonly IReadOnlyList<ModifierOptionChoice> _options;
        readonly string? _originalName;

        public ModifierEntryVm(int optionId = 0, int quantity = 1, string? displayName = null, IReadOnlyList<ModifierOptionChoice>? options = null)
        {
            _options = options ?? Array.Empty<ModifierOptionChoice>();
            _originalName = displayName;
            QuantityText = quantity.ToString();

            if (optionId > 0 && _options.FirstOrDefault(o => o.OptionId == optionId) is ModifierOptionChoice match)
            {
                SelectedOption = match;
            }
            else if (optionId > 0)
            {
                ManualOptionIdText = optionId.ToString();
            }
        }

        public IReadOnlyList<ModifierOptionChoice> AvailableOptions => _options;
        public bool HasOptionCatalog => _options.Count > 0;
        public bool ShowManualOptionId => !HasOptionCatalog;

        ModifierOptionChoice? _selectedOption;
        public ModifierOptionChoice? SelectedOption
        {
            get => _selectedOption;
            set
            {
                if (_selectedOption == value) return;
                _selectedOption = value;
                if (value != null)
                    ManualOptionIdText = value.OptionId.ToString();
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        string _manualOptionIdText = string.Empty;
        public string ManualOptionIdText
        {
            get => _manualOptionIdText;
            set
            {
                if (_manualOptionIdText == value) return;
                _manualOptionIdText = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string DisplayName => SelectedOption?.Display ?? _originalName ?? (ManualOptionIdText.Length > 0 ? $"Opción {ManualOptionIdText}" : "Selecciona un extra");

        string _quantityText = "1";
        public string QuantityText
        {
            get => _quantityText;
            set
            {
                if (_quantityText == value) return;
                _quantityText = value ?? "1";
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public bool TryGetOptionId(out int optionId)
        {
            if (SelectedOption != null)
            {
                optionId = SelectedOption.OptionId;
                return true;
            }

            return int.TryParse(ManualOptionIdText, out optionId) && optionId > 0;
        }

        public bool TryGetQuantity(out int quantity)
        {
            if (!int.TryParse(QuantityText, out quantity))
                return false;
            if (quantity <= 0)
                quantity = 1;
            return true;
        }
    }
}
