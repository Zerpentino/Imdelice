using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using Imdeliceapp;
using Imdeliceapp.Models;
using Imdeliceapp.Pages;
using Imdeliceapp.Services;
using Microsoft.Maui.ApplicationModel;
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
        TakeOrderPage.CartEntry? existingEntry = null,
        Func<int, Task<IReadOnlyList<VariantModifierGroupLinkDTO>>>? variantRulesLoader = null)
    {
        InitializeComponent();
        _viewModel = new ConfigureMenuItemViewModel(baseItem, variants, modifierGroups, existingEntry, CloseWithResult, variantRulesLoader);
        BindingContext = _viewModel;
    }

    void CloseButton_Clicked(object sender, EventArgs e) => Close();

    void CloseWithResult(ConfigureMenuItemResult? result) => Close(result);
}

class ConfigureMenuItemViewModel : INotifyPropertyChanged
{
    readonly Action<ConfigureMenuItemResult?> _closeCallback;
    readonly Guid? _existingLineId;
    readonly Func<int, Task<IReadOnlyList<VariantModifierGroupLinkDTO>>>? _variantRulesLoader;
    readonly Dictionary<int, IReadOnlyList<VariantModifierGroupLinkDTO>> _variantRulesCache = new();

    public ConfigureMenuItemViewModel(
        TakeOrderPage.MenuItemVm baseItem,
        IReadOnlyList<TakeOrderPage.MenuItemVm> variants,
        IReadOnlyList<ModifierGroupDTO> modifierGroups,
        TakeOrderPage.CartEntry? existingEntry,
        Action<ConfigureMenuItemResult?> closeCallback,
        Func<int, Task<IReadOnlyList<VariantModifierGroupLinkDTO>>>? variantRulesLoader)
    {
        BaseItem = baseItem;
        _closeCallback = closeCallback;
        _existingLineId = existingEntry?.LineId;
        _variantRulesLoader = variantRulesLoader;

        Title = baseItem.Title;
        Subtitle = baseItem.Subtitle;
        Description = baseItem.Description;

        var variantChoices = BuildVariants(baseItem, variants, existingEntry?.SelectedItem).ToList();
        Variants = new ObservableCollection<VariantChoiceVm>(variantChoices);

        ModifierGroups = new ObservableCollection<ModifierGroupVm>(
            BuildModifierGroups(modifierGroups, existingEntry));

        var initialVariant = Variants.FirstOrDefault(v => v.IsSelected) ?? Variants.FirstOrDefault();
        SelectedVariant = initialVariant;

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
            if (_variantRulesLoader != null)
                _ = ApplyVariantRulesAsync(_selectedVariant?.Item?.VariantId);
            else
                ApplyVariantRules(null);
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

    async Task ApplyVariantRulesAsync(int? variantId)
    {
        try
        {
            IReadOnlyList<VariantModifierGroupLinkDTO>? overrides = null;
            if (variantId.HasValue && _variantRulesLoader != null)
            {
                if (!_variantRulesCache.TryGetValue(variantId.Value, out overrides))
                {
                    overrides = await _variantRulesLoader(variantId.Value);
                    _variantRulesCache[variantId.Value] = overrides;
                }
            }

            var capturedVariant = variantId;
            var capturedOverrides = overrides;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!Equals(SelectedVariant?.Item?.VariantId, capturedVariant))
                    return;
                ApplyVariantRules(capturedOverrides);
            });
        }
        catch
        {
            var capturedVariant = variantId;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!Equals(SelectedVariant?.Item?.VariantId, capturedVariant))
                    return;
                ApplyVariantRules(null);
            });
        }
    }

    void ApplyVariantRules(IReadOnlyList<VariantModifierGroupLinkDTO>? overrides)
    {
        Dictionary<int, VariantModifierGroupLinkDTO>? map = null;
        if (overrides != null)
        {
            map = overrides
                .Where(o => (o.group?.id ?? o.groupId) != 0)
                .ToDictionary(o => o.group?.id ?? o.groupId);
        }

        foreach (var group in ModifierGroups)
        {
            if (map != null && map.TryGetValue(group.Id, out var rule))
                group.ApplyRules(rule.minSelect, rule.maxSelect, rule.isRequired);
            else
                group.ResetToDefaultRules();
        }
    }

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

class ModifierGroupVm : INotifyPropertyChanged
{
    readonly ModifierGroupDTO _source;
    readonly int _defaultMin;
    readonly int? _defaultMax;
    readonly bool _defaultIsRequired;

    int _min;
    int? _max;
    bool _isRequired;
    bool _isSingleChoice;
    string _requirementText;

    public ModifierGroupVm(ModifierGroupDTO source, TakeOrderPage.CartModifierSelection? existingSelection)
    {
        _source = source;
        _defaultMin = Math.Max(0, source.minSelect);
        _defaultMax = source.maxSelect;
        _defaultIsRequired = source.isRequired;

        _min = _defaultMin;
        _max = _defaultMax;
        _isRequired = _defaultIsRequired;
        _isSingleChoice = _max == 1 && _min <= 1;
        _requirementText = BuildRequirementText();

        Name = source.name;

        Options = new ObservableCollection<ModifierOptionVm>(
            source.options
                .OrderBy(o => o.position)
                .Select(o =>
                {
                    var existingQty = existingSelection?.Options
                        .FirstOrDefault(sel => sel.OptionId == o.id)?.Quantity ?? 0;
                    return new ModifierOptionVm(this, o, existingQty);
                }));
        NotifyLimitChanges();
        RequirementText = BuildRequirementText();
    }

    public int Id => _source.id;
    public string Name { get; }
    public string RequirementText
    {
        get => _requirementText;
        private set
        {
            if (_requirementText == value) return;
            _requirementText = value;
            OnPropertyChanged();
        }
    }
    public ObservableCollection<ModifierOptionVm> Options { get; }

    public bool Validate(out string? message)
    {
        message = null;
        int selectedCount = Options.Sum(o => o.Quantity);

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

    public bool CanIncrementOption(ModifierOptionVm option)
    {
        if (_max.HasValue)
        {
            int total = Options.Sum(o => o.Quantity);
            if (total >= _max.Value)
                return false;
        }
        return true;
    }

    public bool TryIncrementOption(ModifierOptionVm option)
    {
        if (!CanIncrementOption(option))
            return false;

        option.ApplyQuantity(option.Quantity + 1);
        NotifyLimitChanges();
        return true;
    }

    public void DecrementOption(ModifierOptionVm option)
    {
        if (option.Quantity <= 0)
            return;

        option.ApplyQuantity(option.Quantity - 1);
        NotifyLimitChanges();
    }

    void NotifyLimitChanges()
    {
        foreach (var opt in Options)
            opt.NotifyLimitChanged();
    }

    public void ApplyRules(int min, int? max, bool isRequired)
    {
        _min = Math.Max(0, min);
        _max = max;
        _isRequired = isRequired;
        _isSingleChoice = _max == 1 && _min <= 1;
        ClampToMaxIfNeeded();
        RequirementText = BuildRequirementText();
        NotifyLimitChanges();
    }

    public void ResetToDefaultRules()
    {
        _min = _defaultMin;
        _max = _defaultMax;
        _isRequired = _defaultIsRequired;
        _isSingleChoice = _max == 1 && _min <= 1;
        ClampToMaxIfNeeded();
        RequirementText = BuildRequirementText();
        NotifyLimitChanges();
    }

    void ClampToMaxIfNeeded()
    {
        if (!_max.HasValue) return;
        var allowed = Math.Max(0, _max.Value);
        var total = Options.Sum(o => o.Quantity);
        if (total <= allowed) return;

        foreach (var opt in Options.OrderByDescending(o => o.Quantity))
        {
            if (total <= allowed) break;
            var reducible = Math.Min(opt.Quantity, total - allowed);
            if (reducible > 0)
            {
                opt.ApplyQuantity(opt.Quantity - reducible);
                total -= reducible;
            }
        }
    }

    string BuildRequirementText()
    {
        if (_isRequired && _max == 1)
            return "Obligatorio";

        if (_isRequired && _max.HasValue)
            return $"Min {_min} · Max {_max}";

        if (_isRequired)
            return $"Min {_min}";

        if (_max.HasValue)
            return $"Max {_max}";

        return "Opcional";
    }

    public TakeOrderPage.CartModifierSelection? ToCartSelection()
    {
        var selected = Options.Where(o => o.Quantity > 0).ToList();
        if (selected.Count == 0)
            return null;

        var mapped = selected
            .OrderBy(o => o.Id)
            .Select(o => new TakeOrderPage.ModifierOptionSelection(
                o.Id,
                o.Name,
                o.PriceExtra,
                o.Quantity))
            .ToList();

        return new TakeOrderPage.CartModifierSelection(_source.id, _source.name, mapped);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

class ModifierOptionVm : INotifyPropertyChanged
{
    readonly ModifierGroupVm _parent;
    readonly ModifierOptionDTO _source;

    readonly Color _selectedBackground = Color.FromArgb("#f0d8e6");
    readonly Color _defaultBackground = Color.FromArgb("#ffffff");

    int _quantity;
    Color _background;

    public ModifierOptionVm(ModifierGroupVm parent, ModifierOptionDTO source, int initialQuantity)
    {
        _parent = parent;
        _source = source;
        _quantity = Math.Max(0, initialQuantity);
        _background = _quantity > 0 ? _selectedBackground : _defaultBackground;

        Name = source.name;
        Description = source.isDefault ? "Incluido por defecto" : null;
        PriceExtra = source.priceExtraCents.ToCurrency();
        ExtraLabel = PriceExtra > 0 ? $"+{PriceExtra.ToString("$0.00", System.Globalization.CultureInfo.CurrentCulture)}" : "Incluido";
        IncrementCommand = new Command(() =>
        {
            _parent.TryIncrementOption(this);
        }, () => CanIncrement);

        DecrementCommand = new Command(() =>
        {
            _parent.DecrementOption(this);
        }, () => CanDecrement);

        UpdateState();
    }

    public int Id => _source.id;
    public string Name { get; }
    public string? Description { get; }
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public decimal PriceExtra { get; }
    public string ExtraLabel { get; }
    public int Quantity => _quantity;
    public bool IsSelected => Quantity > 0;
    public string QuantityDisplay => Quantity.ToString();
    public bool CanDecrement => Quantity > 0;
    public bool CanIncrement => _parent.CanIncrementOption(this);

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

    internal void ApplyQuantity(int value)
    {
        value = Math.Max(0, value);
        if (_quantity == value) return;
        _quantity = value;
        UpdateState();
    }

    void UpdateState()
    {
        Background = IsSelected ? _selectedBackground : _defaultBackground;
        OnPropertyChanged(nameof(Quantity));
        OnPropertyChanged(nameof(QuantityDisplay));
        OnPropertyChanged(nameof(IsSelected));
        OnPropertyChanged(nameof(CanDecrement));
        OnPropertyChanged(nameof(CanIncrement));
        IncrementCommand.ChangeCanExecute();
        DecrementCommand.ChangeCanExecute();
    }

    public void NotifyLimitChanged()
    {
        OnPropertyChanged(nameof(CanIncrement));
        IncrementCommand.ChangeCanExecute();
    }

    public Command IncrementCommand { get; }
    public Command DecrementCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
