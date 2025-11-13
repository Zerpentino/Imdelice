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

public record ComboChildConfiguration(
        TakeOrderPage.MenuItemVm.ComboComponent Component,
        IReadOnlyList<TakeOrderPage.MenuItemVm> Variants,
        IReadOnlyList<ModifierGroupDTO> ModifierGroups,
        Func<int, Task<IReadOnlyList<VariantModifierGroupLinkDTO>>>? VariantRulesLoader,
        TakeOrderPage.ComboChildSelection? ExistingSelection,
        bool AllowVariantSelection,
        string? DisplayNotes);

public partial class ConfigureMenuItemPopup : Popup
{
    readonly ConfigureMenuItemViewModel _viewModel;

    public ConfigureMenuItemPopup(
        TakeOrderPage.MenuItemVm baseItem,
        IReadOnlyList<TakeOrderPage.MenuItemVm> variants,
        IReadOnlyList<ModifierGroupDTO> modifierGroups,
        TakeOrderPage.CartEntry? existingEntry = null,
        Func<int, Task<IReadOnlyList<VariantModifierGroupLinkDTO>>>? variantRulesLoader = null,
        bool lockQuantity = false,
        int? lockedQuantity = null,
        string? confirmButtonText = null,
        IReadOnlyList<ComboChildConfiguration>? comboChildConfigurations = null,
        bool showBackButton = false)
    {
        InitializeComponent();
        _viewModel = new ConfigureMenuItemViewModel(
            baseItem,
            variants,
            modifierGroups,
            existingEntry,
            CloseWithResult,
            variantRulesLoader,
            lockQuantity,
            lockedQuantity,
            confirmButtonText,
            comboChildConfigurations,
            showBackButton);
        BindingContext = _viewModel;
    }

    void CloseButton_Clicked(object sender, EventArgs e) => Close();

    public bool BackRequested { get; private set; }

    void BackButton_Clicked(object sender, EventArgs e)
    {
        BackRequested = true;
        Close();
    }

    void CloseWithResult(ConfigureMenuItemResult? result) => Close(result);
}

class ConfigureMenuItemViewModel : INotifyPropertyChanged
{
    readonly Action<ConfigureMenuItemResult?> _closeCallback;
    readonly Guid? _existingLineId;
    readonly Func<int, Task<IReadOnlyList<VariantModifierGroupLinkDTO>>>? _variantRulesLoader;
    readonly Dictionary<int, IReadOnlyList<VariantModifierGroupLinkDTO>> _variantRulesCache = new();
    readonly bool _isQuantityLocked;
    readonly double? _lockedQuantityValue;
    readonly string? _confirmButtonTextOverride;
    readonly IReadOnlyList<ComboChildConfiguration>? _comboChildConfigurations;

    public ConfigureMenuItemViewModel(
        TakeOrderPage.MenuItemVm baseItem,
        IReadOnlyList<TakeOrderPage.MenuItemVm> variants,
        IReadOnlyList<ModifierGroupDTO> modifierGroups,
        TakeOrderPage.CartEntry? existingEntry,
        Action<ConfigureMenuItemResult?> closeCallback,
        Func<int, Task<IReadOnlyList<VariantModifierGroupLinkDTO>>>? variantRulesLoader,
        bool lockQuantity,
        int? lockedQuantity,
        string? confirmButtonText,
        IReadOnlyList<ComboChildConfiguration>? comboChildConfigurations,
        bool showBackButton)
    {
        BaseItem = baseItem;
        _closeCallback = closeCallback;
        _existingLineId = existingEntry?.LineId;
        _variantRulesLoader = variantRulesLoader;
        _confirmButtonTextOverride = confirmButtonText;
        _isQuantityLocked = lockQuantity || (lockedQuantity.HasValue && lockedQuantity.Value > 0);
        _lockedQuantityValue = lockedQuantity.HasValue && lockedQuantity.Value > 0
            ? Math.Max(1, lockedQuantity.Value)
            : (_isQuantityLocked ? existingEntry?.Quantity : null);
        _comboChildConfigurations = comboChildConfigurations;
        ShowBackButton = showBackButton;

        Title = baseItem.Title;
        Subtitle = baseItem.Subtitle;
        Description = baseItem.Description;

        var variantChoices = BuildVariants(baseItem, variants, existingEntry?.SelectedItem).ToList();
        Variants = new ObservableCollection<VariantChoiceVm>(variantChoices);

        ModifierGroups = new ObservableCollection<ModifierGroupVm>(
            BuildModifierGroups(modifierGroups, existingEntry));

        ComboChildren = new ObservableCollection<ComboChildVm>(
            BuildComboChildren(BaseItem, existingEntry, comboChildConfigurations));

        var initialVariant = Variants.FirstOrDefault(v => v.IsSelected) ?? Variants.FirstOrDefault();
        SelectedVariant = initialVariant;

        var initialQuantity = _lockedQuantityValue ?? (existingEntry?.Quantity ?? 1);
        Quantity = initialQuantity;
        Notes = existingEntry?.Notes ?? string.Empty;

        ConfirmCommand = new Command(Confirm);
        CancelCommand = new Command(() => _closeCallback(null));

        OnPropertyChanged(nameof(HasVariants));
        OnPropertyChanged(nameof(HasModifiers));
        OnPropertyChanged(nameof(HasComboChildren));
        OnPropertyChanged(nameof(ConfirmButtonText));
    }

    public TakeOrderPage.MenuItemVm BaseItem { get; }

    public string Title { get; }
    public string? Subtitle { get; }
    public string? Description { get; }
    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public bool ShowBackButton { get; }

    public ObservableCollection<VariantChoiceVm> Variants { get; }

    public bool HasVariants => !BaseItem.IsCombo && Variants.Count > 1;

    public ObservableCollection<ComboChildVm> ComboChildren { get; }
    public bool HasComboChildren => ComboChildren.Count > 0;

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

    public bool IsQuantityLocked => _isQuantityLocked;
    public bool IsQuantityEditable => !_isQuantityLocked;

    double _quantity = 1;
    public double Quantity
    {
        get => _quantity;
        set
        {
            var newValue = Math.Max(1, Math.Round(value));
            if (_isQuantityLocked)
                newValue = _lockedQuantityValue ?? newValue;
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
    public string ConfirmButtonText => _confirmButtonTextOverride
        ?? (_existingLineId.HasValue ? "Actualizar" : "Agregar al carrito");

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

        foreach (var child in ComboChildren)
        {
            if (!child.IsSelected)
            {
                if (child.IsRequired)
                {
                    ValidationMessage = "Selecciona todos los elementos requeridos del combo.";
                    return;
                }
                continue;
            }

            if (!child.TryValidate(out var childMessage))
            {
                ValidationMessage = childMessage;
                return;
            }
        }

        var selectedModifiers = ModifierGroups
            .Select(group => group.ToCartSelection())
            .Where(selection => selection != null)
            .Select(selection => selection!)
            .ToList();

        var selectedChildren = ComboChildren
            .Select(child => child.ToSelection())
            .Where(selection => selection != null)
            .Select(selection => selection!)
            .ToList();

        var result = new ConfigureMenuItemResult(
            BaseItem,
            SelectedVariant.Item,
            (int)Quantity,
            Notes?.Trim(),
            selectedModifiers,
            selectedChildren,
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

    static IEnumerable<ComboChildVm> BuildComboChildren(
        TakeOrderPage.MenuItemVm baseItem,
        TakeOrderPage.CartEntry? existingEntry,
        IReadOnlyList<ComboChildConfiguration>? comboChildConfigurations)
    {
        if (baseItem.ComboComponents == null || baseItem.ComboComponents.Count == 0)
            return Enumerable.Empty<ComboChildVm>();

        var selections = existingEntry?.ComboChildren?
            .GroupBy(c => (c.ProductId, c.VariantId))
            .ToDictionary(g => g.Key, g => g.First())
            ?? new Dictionary<(int, int?), TakeOrderPage.ComboChildSelection>();

        var configMap = comboChildConfigurations?
            .GroupBy(c => (c.Component.ProductId, c.Component.VariantId))
            .ToDictionary(g => g.Key, g => g.First());

        var list = new List<ComboChildVm>();
        foreach (var component in baseItem.ComboComponents)
        {
            if (component.ProductId <= 0)
                continue;

            ComboChildConfiguration? config = null;
            configMap?.TryGetValue((component.ProductId, component.VariantId), out config);

            selections.TryGetValue((component.ProductId, component.VariantId), out var existing);
            if (existing == null)
                existing = selections.Values.FirstOrDefault(s => s.ProductId == component.ProductId);
            list.Add(new ComboChildVm(component, config, existing ?? config?.ExistingSelection));
        }

        return list;
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

class ComboChildVm : INotifyPropertyChanged
{
    readonly TakeOrderPage.MenuItemVm.ComboComponent _component;
    readonly Func<int, Task<IReadOnlyList<VariantModifierGroupLinkDTO>>>? _variantRulesLoader;
    readonly Dictionary<int, IReadOnlyList<VariantModifierGroupLinkDTO>> _variantRulesCache = new();

    bool _isSelected;
    VariantChoiceVm? _selectedVariant;

    public ComboChildVm(
        TakeOrderPage.MenuItemVm.ComboComponent component,
        ComboChildConfiguration? config,
        TakeOrderPage.ComboChildSelection? existing)
    {
        _component = component;
        _variantRulesLoader = config?.VariantRulesLoader;
        Quantity = existing?.Quantity > 0
            ? existing.Quantity
            : (component.Quantity <= 0 ? 1 : component.Quantity);
        IsRequired = component.IsRequired;
        Notes = existing?.Notes ?? config?.DisplayNotes ?? component.Notes;

        var variantChoices = BuildVariantChoices(
            component,
            config?.Variants,
            existing?.VariantId ?? component.VariantId).ToList();
        Variants = new ObservableCollection<VariantChoiceVm>(variantChoices);
        var initialVariant = Variants.FirstOrDefault(v => v.IsSelected) ?? Variants.FirstOrDefault();
        if (initialVariant != null)
            SelectedVariant = initialVariant;

        ModifierGroups = new ObservableCollection<ModifierGroupVm>(
            BuildChildModifierGroups(config?.ModifierGroups, existing));

        _isSelected = existing != null ? existing.Quantity > 0 : true;

        if (initialVariant == null)
            _ = ApplyVariantRulesAsync(component.VariantId);
    }

    public ObservableCollection<VariantChoiceVm> Variants { get; }
    public ObservableCollection<ModifierGroupVm> ModifierGroups { get; }

    public bool HasVariants => Variants.Count > 1;
    public bool HasModifiers => ModifierGroups.Count > 0;

    public VariantChoiceVm? SelectedVariant
    {
        get => _selectedVariant;
        set
        {
            if (_selectedVariant == value)
                return;

            if (_selectedVariant != null)
                _selectedVariant.IsSelected = false;

            _selectedVariant = value;

            if (_selectedVariant != null)
                _selectedVariant.IsSelected = true;

            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(VariantSummary));
            OnPropertyChanged(nameof(HasVariantSummary));
            _ = ApplyVariantRulesAsync(_selectedVariant?.Item?.VariantId ?? _component.VariantId);
        }
    }

    public int Quantity { get; }
    public bool IsRequired { get; }
    public string? Notes { get; }

    public bool CanToggle => !IsRequired;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            var normalized = value || IsRequired;
            if (_isSelected == normalized) return;
            _isSelected = normalized;
            OnPropertyChanged();
        }
    }

    public string DisplayName
    {
        get
        {
            var variant = SelectedVariant?.Item;
            if (variant != null)
            {
                var subtitle = variant.Subtitle;
                if (!string.IsNullOrWhiteSpace(subtitle) && !string.Equals(subtitle, variant.Title, StringComparison.OrdinalIgnoreCase))
                    return $"{variant.Title} · {subtitle}";
                return variant.Title;
            }

            if (!string.IsNullOrWhiteSpace(_component.VariantName))
                return $"{_component.ProductName} · {_component.VariantName}";

            return _component.ProductName;
        }
    }

    public string? VariantSummary
    {
        get
        {
            if (SelectedVariant?.Item != null)
                return SelectedVariant.Item.Title;
            if (!string.IsNullOrWhiteSpace(_component.VariantName))
                return _component.VariantName;
            if (!string.IsNullOrWhiteSpace(_component.Notes))
                return _component.Notes;
            return null;
        }
    }

    public bool HasVariantSummary => !string.IsNullOrWhiteSpace(VariantSummary);

    public string? HeaderDetail
    {
        get
        {
            var parts = new List<string>();
            if (!IsRequired)
                parts.Add("Opcional");
            if (Quantity > 1)
                parts.Add($"Cantidad: {Quantity}");
            if (!string.IsNullOrWhiteSpace(Notes))
                parts.Add(Notes!);
            return parts.Count == 0 ? null : string.Join(" • ", parts);
        }
    }

    public bool HasHeaderDetail => !string.IsNullOrWhiteSpace(HeaderDetail);

    static IEnumerable<VariantChoiceVm> BuildVariantChoices(
        TakeOrderPage.MenuItemVm.ComboComponent component,
        IReadOnlyList<TakeOrderPage.MenuItemVm>? variants,
        int? selectedVariantId)
    {
        if (variants == null || variants.Count == 0)
            yield break;

        foreach (var vm in variants.OrderBy(v => v.DisplaySortOrder).ThenBy(v => v.Title))
        {
            var isSelected = selectedVariantId.HasValue
                ? vm.VariantId == selectedVariantId
                : vm.Id == variants.First().Id;
            yield return new VariantChoiceVm(vm, isSelected);
        }
    }

    static IEnumerable<ModifierGroupVm> BuildChildModifierGroups(
        IReadOnlyList<ModifierGroupDTO>? modifierGroups,
        TakeOrderPage.ComboChildSelection? existingSelection)
    {
        if (modifierGroups == null || modifierGroups.Count == 0)
            yield break;

        var existing = existingSelection?.Modifiers?
            .ToDictionary(m => m.GroupId);

        foreach (var group in modifierGroups.OrderBy(g => g.position))
        {
            TakeOrderPage.CartModifierSelection? selection = null;
            if (existing != null)
                existing.TryGetValue(group.id, out selection);
            yield return new ModifierGroupVm(group, selection);
        }
    }

    async Task ApplyVariantRulesAsync(int? variantId)
    {
        IReadOnlyList<VariantModifierGroupLinkDTO>? overrides = null;

        if (_variantRulesLoader != null && variantId.HasValue)
        {
            if (!_variantRulesCache.TryGetValue(variantId.Value, out overrides))
            {
                try
                {
                    overrides = await _variantRulesLoader(variantId.Value);
                }
                catch
                {
                    overrides = Array.Empty<VariantModifierGroupLinkDTO>();
                }
                _variantRulesCache[variantId.Value] = overrides;
            }
        }

        var capturedVariant = variantId;
        var capturedOverrides = overrides;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (SelectedVariant?.Item?.VariantId != capturedVariant)
                return;
            ApplyVariantRules(capturedOverrides);
        });
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

    public bool TryValidate(out string? message)
    {
        message = null;
        if (!IsSelected)
            return true;

        foreach (var group in ModifierGroups)
        {
            if (!group.Validate(out message))
                return false;
        }

        return true;
    }

    public TakeOrderPage.ComboChildSelection? ToSelection()
    {
        if (!IsSelected)
            return null;

        var variantItem = SelectedVariant?.Item;
        var productName = variantItem?.Title ?? _component.ProductName;
        var variantName = variantItem?.VariantId.HasValue == true
            ? variantItem.Title
            : variantItem?.Subtitle ?? _component.VariantName;

        var selections = ModifierGroups
            .Select(group => group.ToCartSelection())
            .Where(selection => selection != null)
            .Select(selection => selection!)
            .ToList();

        return new TakeOrderPage.ComboChildSelection(
            _component.ProductId,
            variantItem?.VariantId ?? _component.VariantId,
            Quantity,
            IsRequired,
            productName,
            variantName,
            string.IsNullOrWhiteSpace(Notes) ? null : Notes,
            selections);
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
    bool _isExpanded;

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
        ToggleExpandedCommand = new Command(() => IsExpanded = !IsExpanded);
        IsExpanded = Options.Any(o => o.Quantity > 0);
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
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            OnPropertyChanged();
        }
    }

    public ICommand ToggleExpandedCommand { get; }

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
