using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using Imdeliceapp.Models;
using Imdeliceapp.Pages;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Imdeliceapp.Popups;

public partial class ConfigureMenuItemPopup : Popup
{
    readonly ConfigureMenuItemViewModel _viewModel;

    public ConfigureMenuItemPopup(
        TakeOrderPage.MenuItemVm baseItem,
        IReadOnlyList<TakeOrderPage.MenuItemVm> variants,
        IReadOnlyList<ModifierGroupDTO> modifierGroups,
        TakeOrderPage.CartEntry? existingEntry = null)
    {
        InitializeComponent();
        _viewModel = new ConfigureMenuItemViewModel(baseItem, variants, modifierGroups, existingEntry, CloseWithResult);
        BindingContext = _viewModel;
    }

    void CloseButton_Clicked(object sender, EventArgs e) => Close();

    void CloseWithResult(ConfigureMenuItemResult? result) => Close(result);
}

class ConfigureMenuItemViewModel : INotifyPropertyChanged
{
    readonly Action<ConfigureMenuItemResult?> _closeCallback;
    readonly Guid? _existingLineId;

    public ConfigureMenuItemViewModel(
        TakeOrderPage.MenuItemVm baseItem,
        IReadOnlyList<TakeOrderPage.MenuItemVm> variants,
        IReadOnlyList<ModifierGroupDTO> modifierGroups,
        TakeOrderPage.CartEntry? existingEntry,
        Action<ConfigureMenuItemResult?> closeCallback)
    {
        BaseItem = baseItem;
        _closeCallback = closeCallback;
        _existingLineId = existingEntry?.LineId;

        Title = baseItem.Title;
        Subtitle = baseItem.Subtitle;
        Description = baseItem.Description;

        Variants = new ObservableCollection<VariantChoiceVm>(
            BuildVariants(baseItem, variants, existingEntry?.SelectedItem));
        SelectedVariant = Variants.FirstOrDefault(v => v.IsSelected) ?? Variants.FirstOrDefault();

        ModifierGroups = new ObservableCollection<ModifierGroupVm>(
            BuildModifierGroups(modifierGroups, existingEntry));

        Quantity = existingEntry?.Quantity ?? 1;
        Notes = existingEntry?.Notes ?? string.Empty;

        ConfirmCommand = new Command(Confirm);
        CancelCommand = new Command(() => _closeCallback(null));

        OnPropertyChanged(nameof(HasVariants));
        OnPropertyChanged(nameof(HasModifiers));
        OnPropertyChanged(nameof(ConfirmButtonText));
    }

    public TakeOrderPage.MenuItemVm BaseItem { get; }

    public string Title { get; }
    public string? Subtitle { get; }
    public string? Description { get; }
    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    public ObservableCollection<VariantChoiceVm> Variants { get; }

    public bool HasVariants => Variants.Count > 1;

    VariantChoiceVm? _selectedVariant;
    public VariantChoiceVm? SelectedVariant
    {
        get => _selectedVariant;
        set
        {
            if (_selectedVariant == value) return;
            if (_selectedVariant != null)
                _selectedVariant.IsSelected = false;

            _selectedVariant = value;
            if (_selectedVariant != null)
                _selectedVariant.IsSelected = true;

            OnPropertyChanged();
        }
    }

    public ObservableCollection<ModifierGroupVm> ModifierGroups { get; }

    public bool HasModifiers => ModifierGroups.Count > 0;

    double _quantity = 1;
    public double Quantity
    {
        get => _quantity;
        set
        {
            var newValue = Math.Max(1, Math.Round(value));
            if (Math.Abs(_quantity - newValue) < double.Epsilon) return;
            _quantity = newValue;
            OnPropertyChanged();
        }
    }

    string? _notes;
    public string? Notes
    {
        get => _notes;
        set
        {
            if (_notes == value) return;
            _notes = value;
            OnPropertyChanged();
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
    public string ConfirmButtonText => _existingLineId.HasValue ? "Actualizar" : "Agregar al carrito";

    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }

    void Confirm()
    {
        ValidationMessage = null;

        if (SelectedVariant == null)
        {
            ValidationMessage = "Selecciona una presentación.";
            return;
        }

        foreach (var group in ModifierGroups)
        {
            if (!group.Validate(out var message))
            {
                ValidationMessage = message;
                return;
            }
        }

        var selectedModifiers = ModifierGroups
            .Select(group => group.ToCartSelection())
            .Where(selection => selection != null)
            .Select(selection => selection!)
            .ToList();

        var result = new ConfigureMenuItemResult(
            BaseItem,
            SelectedVariant.Item,
            (int)Quantity,
            Notes?.Trim(),
            selectedModifiers,
            _existingLineId);

        _closeCallback(result);
    }

    static IEnumerable<VariantChoiceVm> BuildVariants(
        TakeOrderPage.MenuItemVm baseItem,
        IReadOnlyList<TakeOrderPage.MenuItemVm> variants,
        TakeOrderPage.MenuItemVm? selected)
    {
        if (variants == null || variants.Count == 0)
        {
            yield return new VariantChoiceVm(baseItem, isSelected: true);
            yield break;
        }

        foreach (var item in variants.OrderBy(v => v.DisplaySortOrder).ThenBy(v => v.Title))
        {
            var isSelected = selected != null
                ? item.Id == selected.Id
                : item.Id == baseItem.Id;

            yield return new VariantChoiceVm(item, isSelected);
        }
    }

    static IEnumerable<ModifierGroupVm> BuildModifierGroups(
        IReadOnlyList<ModifierGroupDTO> modifierGroups,
        TakeOrderPage.CartEntry? existingEntry)
    {
        if (modifierGroups == null || modifierGroups.Count == 0)
            yield break;

        foreach (var group in modifierGroups.OrderBy(g => g.position))
        {
            var existingSelection = existingEntry?.Modifiers.FirstOrDefault(m => m.GroupId == group.id);
            yield return new ModifierGroupVm(group, existingSelection);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

class VariantChoiceVm : INotifyPropertyChanged
{
    readonly Color _selectedBackground = Color.FromArgb("#f0d8e6");
    readonly Color _defaultBackground = Color.FromArgb("#ffffff");

    public VariantChoiceVm(TakeOrderPage.MenuItemVm item, bool isSelected)
    {
        Item = item;
        DisplayName = item.Title;
        Description = item.Subtitle;
        PriceLabel = item.PriceLabel;
        _isSelected = isSelected;

        UpdateBackground();
    }

    public TakeOrderPage.MenuItemVm Item { get; }
    public string DisplayName { get; }
    public string? Description { get; }
    public string PriceLabel { get; }
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            UpdateBackground();
            OnPropertyChanged();
        }
    }

    Color _background = Colors.White;
    public Color BackgroundColor
    {
        get => _background;
        private set
        {
            if (_background == value) return;
            _background = value;
            OnPropertyChanged();
        }
    }

    void UpdateBackground()
    {
        BackgroundColor = IsSelected ? _selectedBackground : _defaultBackground;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

class ModifierGroupVm
{
    readonly ModifierGroupDTO _source;
    readonly bool _isSingleChoice;
    readonly int _min;
    readonly int? _max;

    public ModifierGroupVm(ModifierGroupDTO source, TakeOrderPage.CartModifierSelection? existingSelection)
    {
        _source = source;
        _min = Math.Max(0, source.minSelect);
        _max = source.maxSelect;
        _isSingleChoice = _max == 1 && _min <= 1;

        Name = source.name;
        RequirementText = BuildRequirementText(source);

        Options = new ObservableCollection<ModifierOptionVm>(
            source.options
                .OrderBy(o => o.position)
                .Select(o => new ModifierOptionVm(this, o,
                    existingSelection?.Options.Any(sel => sel.OptionId == o.id) ?? false)));
    }

    public string Name { get; }
    public string RequirementText { get; }
    public ObservableCollection<ModifierOptionVm> Options { get; }

    public bool Validate(out string? message)
    {
        message = null;
        int selectedCount = Options.Count(o => o.IsSelected);

        if (selectedCount < _min)
        {
            message = _min == 1
                ? $"Selecciona al menos una opción en {Name}."
                : $"Selecciona al menos {_min} opciones en {Name}.";
            return false;
        }

        if (_max.HasValue && selectedCount > _max.Value)
        {
            message = $"Selecciona máximo {_max.Value} opciones en {Name}.";
            return false;
        }

        return true;
    }

    public void ToggleOption(ModifierOptionVm option)
    {
        if (_isSingleChoice)
        {
            foreach (var opt in Options)
                opt.SetSelected(opt == option);
            return;
        }

        if (!option.IsSelected)
        {
            if (_max.HasValue && Options.Count(o => o.IsSelected) >= _max.Value)
                return;
            option.SetSelected(true);
        }
        else
        {
            option.SetSelected(false);
        }
    }

    static string BuildRequirementText(ModifierGroupDTO source)
    {
        if (source.isRequired && source.maxSelect == 1)
            return "Obligatorio";

        if (source.isRequired && source.maxSelect.HasValue)
            return $"Min {source.minSelect} · Max {source.maxSelect}";

        if (source.isRequired)
            return $"Min {source.minSelect}";

        if (source.maxSelect.HasValue)
            return $"Max {source.maxSelect}";

        return "Opcional";
    }

    public TakeOrderPage.CartModifierSelection? ToCartSelection()
    {
        var selected = Options.Where(o => o.IsSelected).ToList();
        if (selected.Count == 0)
            return null;

        var mapped = selected
            .OrderBy(o => o.Id)
            .Select(o => new TakeOrderPage.ModifierOptionSelection(
                o.Id,
                o.Name,
                o.PriceExtra))
            .ToList();

        return new TakeOrderPage.CartModifierSelection(_source.id, _source.name, mapped);
    }
}

class ModifierOptionVm : INotifyPropertyChanged
{
    readonly ModifierGroupVm _parent;
    readonly ModifierOptionDTO _source;

    readonly Color _selectedBackground = Color.FromArgb("#f0d8e6");
    readonly Color _defaultBackground = Color.FromArgb("#ffffff");

    bool _isSelected;
    Color _background;

    public ModifierOptionVm(ModifierGroupVm parent, ModifierOptionDTO source, bool isSelected)
    {
        _parent = parent;
        _source = source;
        _isSelected = isSelected;
        _background = isSelected ? _selectedBackground : _defaultBackground;

        Name = source.name;
        Description = source.isDefault ? "Incluido por defecto" : null;
        PriceExtra = source.priceExtraCents.ToCurrency();
        ExtraLabel = PriceExtra > 0 ? $"+{PriceExtra.ToString("$0.00", System.Globalization.CultureInfo.CurrentCulture)}" : "Incluido";

        ToggleCommand = new Command(() => _parent.ToggleOption(this));
    }

    public int Id => _source.id;
    public string Name { get; }
    public string? Description { get; }
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public decimal PriceExtra { get; }
    public string ExtraLabel { get; }

    public bool IsSelected
    {
        get => _isSelected;
        private set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            Background = value ? _selectedBackground : _defaultBackground;
            OnPropertyChanged();
        }
    }

    public Color Background
    {
        get => _background;
        private set
        {
            if (_background == value) return;
            _background = value;
            OnPropertyChanged();
        }
    }

    public void SetSelected(bool value) => IsSelected = value;

    public ICommand ToggleCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
