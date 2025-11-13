using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using Imdeliceapp.Models;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;

namespace Imdeliceapp.Popups;

public partial class OrderSplitPopup : Popup
{
    readonly OrdersApi _ordersApi = new();
    readonly ObservableCollection<ServiceTypeOption> _serviceTypeOptions = new();
    readonly ObservableCollection<TableOptionVm> _tableOptions = new();

    readonly Dictionary<int, List<SplitItemVm>> _childrenLookup = new();
    public ObservableCollection<SplitItemVm> Items { get; } = new();

    public OrderSplitPopup(IEnumerable<OrderItemDTO> rootItems, string? currentServiceType, int? currentTableId, int? currentCovers, string? currentTableName)
    {
        InitializeComponent();
        BindingContext = this;

        ServiceTypePicker.ItemsSource = _serviceTypeOptions;
        TablePicker.ItemsSource = _tableOptions;
        TablePicker.Title = string.IsNullOrWhiteSpace(currentTableName)
            ? "Mantener la mesa actual"
            : $"Actual: {currentTableName}";

        BuildItems(rootItems ?? Array.Empty<OrderItemDTO>());
        BuildServiceOptions(currentServiceType);
        _ = LoadTablesAsync(currentTableId);

        if (currentCovers.HasValue)
            CoversEntry.Text = currentCovers.Value.ToString();
    }

    void BuildItems(IEnumerable<OrderItemDTO> rootItems)
    {
        Items.Clear();
        _childrenLookup.Clear();
        foreach (var item in rootItems)
        {
            foreach (var vm in Flatten(item, 0, null))
            {
                RegisterItem(vm);
            }
        }
    }

    static IEnumerable<SplitItemVm> Flatten(OrderItemDTO item, int level, OrderItemDTO? parent)
    {
        yield return SplitItemVm.From(item, level, parent);

        if (item.childItems == null)
            yield break;

        foreach (var child in item.childItems)
        {
            foreach (var vm in Flatten(child, level + 1, item))
                yield return vm;
        }
    }

    void BuildServiceOptions(string? currentServiceType)
    {
        _serviceTypeOptions.Clear();
        var options = new List<ServiceTypeOption>
        {
            new(null, "Mantener tipo actual"),
            new("DINE_IN", "Servicio en mesa"),
            new("TAKEAWAY", "Para llevar"),
            new("DELIVERY", "Entrega a domicilio")
        };

        foreach (var option in options)
            _serviceTypeOptions.Add(option);
        ServiceTypePicker.SelectedItem = _serviceTypeOptions.First();

        if (!string.IsNullOrWhiteSpace(currentServiceType))
            ServiceTypePicker.Title = $"Actual: {GetServiceTypeLabel(currentServiceType)}";
    }

    static string GetServiceTypeLabel(string? code) => code?.ToUpperInvariant() switch
    {
        "DINE_IN" => "Servicio en mesa",
        "TAKEAWAY" => "Para llevar",
        "DELIVERY" => "Entrega a domicilio",
        _ => "Usar el del pedido original"
    };

    async Task LoadTablesAsync(int? currentTableId)
    {
        try
        {
        _tableOptions.Clear();
        _tableOptions.Add(TableOptionVm.KeepCurrent());
        _tableOptions.Add(TableOptionVm.NoTable());

            var tables = await _ordersApi.ListTablesAsync(includeInactive: true);
            foreach (var dto in tables
                         .OrderByDescending(t => t.isActive)
                         .ThenBy(t => t.name ?? $"Mesa {t.id}"))
            {
                _tableOptions.Add(TableOptionVm.From(dto));
            }

            TablePicker.SelectedItem = _tableOptions.First();
        }
        catch
        {
            if (_tableOptions.Count == 0)
            {
                _tableOptions.Add(TableOptionVm.KeepCurrent());
            }
            TablePicker.SelectedItem = _tableOptions.First();
        }
    }

    void Cancel_Clicked(object sender, EventArgs e) => Close(null);

    void Split_Clicked(object sender, EventArgs e)
    {
        ValidationLabel.IsVisible = false;
        ValidationLabel.Text = string.Empty;

        var selections = Items
            .Where(i => i.IsRoot && i.SelectedQuantity > 0)
            .ToList();

        if (selections.Count == 0)
        {
            ShowError("Selecciona al menos un producto.");
            return;
        }

        if (!TryParseNullableInt(CoversEntry.Text, out var covers))
        {
            ShowError("Los comensales deben ser un número válido.");
            return;
        }

        var tableOption = TablePicker.SelectedItem as TableOptionVm;
        var serviceOption = ServiceTypePicker.SelectedItem as ServiceTypeOption;
        var note = string.IsNullOrWhiteSpace(NoteEditor.Text) ? null : NoteEditor.Text.Trim();

        var dto = new OrderSplitRequestDto
        {
            itemIds = selections.Select(s => s.Source.id).ToList(),
            serviceType = serviceOption?.Value,
            note = note,
            covers = covers,
            tableId = tableOption?.Id,
            SendTableId = tableOption != null && tableOption.Override
        };

        var plan = new SplitPlanResult
        {
            Request = dto,
            Items = selections.Select(s => new ItemSelection
            {
                ItemId = s.Source.id,
                OriginalQuantity = s.MaxQuantity,
                SelectedQuantity = s.IsComboParent ? s.MaxQuantity : s.SelectedQuantity,
                AllowPartial = s.AllowPartial
            }).ToList()
        };

        Close(plan);
    }

    static bool TryParseNullableInt(string? text, out int? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text))
            return true;

        if (int.TryParse(text.Trim(), out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    void ShowError(string message)
    {
        ValidationLabel.Text = message;
        ValidationLabel.IsVisible = true;
    }

    void RegisterItem(SplitItemVm vm)
    {
        Items.Add(vm);
        if (vm.ParentId.HasValue)
        {
            if (!_childrenLookup.TryGetValue(vm.ParentId.Value, out var list))
                list = _childrenLookup[vm.ParentId.Value] = new List<SplitItemVm>();
            list.Add(vm);
        }
    }

    void UpdateChildSelections(SplitItemVm parent)
    {
        if (!_childrenLookup.TryGetValue(parent.Source.id, out var children))
            return;

        foreach (var child in children)
            child.SetFromParent(parent.IsSelected);
    }

    void ItemCheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if ((sender as CheckBox)?.BindingContext is not SplitItemVm vm)
            return;
        if (!vm.IsSelectable)
            return;

        vm.IsSelected = e.Value;
        UpdateChildSelections(vm);
    }

    void DecreaseQuantity_Clicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not SplitItemVm vm)
            return;
        if (!vm.HasQuantityPicker)
            return;

        vm.SelectedQuantity -= 1;
        UpdateChildSelections(vm);
    }

    void IncreaseQuantity_Clicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not SplitItemVm vm)
            return;
        if (!vm.HasQuantityPicker)
            return;

        vm.SelectedQuantity += 1;
        UpdateChildSelections(vm);
    }

    public class SplitItemVm : BindableObject
    {
        bool _isSelected;
        int _selectedQuantity;

        public required OrderItemDTO Source { get; init; }
        public required string DisplayTitle { get; init; }
        public required string Detail { get; init; }
        public required Thickness RowMargin { get; init; }
        public required bool IsSelectable { get; init; }
        public required bool IsChild { get; init; }
        public required bool IsComboParent { get; init; }
        public required int MaxQuantity { get; init; }
        public bool AllowPartial { get; init; }
        public bool IsRoot => !IsChild;
        public int? ParentId { get; init; }
        public bool HasQuantityPicker => AllowPartial && MaxQuantity > 1;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;
                _isSelected = value;
                if (!IsChild)
                {
                    if (_isSelected && SelectedQuantity == 0)
                        SelectedQuantity = AllowPartial ? 1 : MaxQuantity;
                    if (!_isSelected)
                        SelectedQuantity = 0;
                }
                OnPropertyChanged();
            }
        }

        public int SelectedQuantity
        {
            get => _selectedQuantity;
            set
            {
                var clamped = Math.Max(0, Math.Min(MaxQuantity, value));
                if (_selectedQuantity == clamped)
                    return;
                _selectedQuantity = clamped;
                OnPropertyChanged();
                OnPropertyChanged(nameof(QuantityLabel));
                var shouldSelect = _selectedQuantity > 0;
                if (_isSelected != shouldSelect)
                {
                    _isSelected = shouldSelect;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public string QuantityLabel => HasQuantityPicker
            ? $"{SelectedQuantity}/{MaxQuantity}"
            : string.Empty;

        public static SplitItemVm From(OrderItemDTO item, int level, OrderItemDTO? parent)
        {
            var detailParts = new List<string> { $"{item.quantity}×" };

            if (!string.IsNullOrWhiteSpace(item.status))
                detailParts.Add(item.status);
            if (!string.IsNullOrWhiteSpace(item.notes))
                detailParts.Add($"Nota: {item.notes}");
            if (parent != null)
            {
                var parentName = parent.nameSnapshot ?? parent.product?.name ?? $"Combo #{parent.id}";
                detailParts.Add($"Combo: {parentName}");
            }

            var isSelectable = parent == null && !string.Equals(item.status, "CANCELED", StringComparison.OrdinalIgnoreCase);
            var isComboParent = item.childItems != null && item.childItems.Count > 0;
            var allowPartial = isSelectable && !isComboParent;

            return new SplitItemVm
            {
                Source = item,
                DisplayTitle = item.nameSnapshot ?? item.product?.name ?? $"Producto {item.productId}",
                Detail = string.Join(" · ", detailParts),
                RowMargin = new Thickness(level * 16, 6, 0, 6),
                IsSelectable = isSelectable,
                IsSelected = false,
                IsChild = parent != null,
                IsComboParent = isComboParent,
                MaxQuantity = Math.Max(1, item.quantity),
                AllowPartial = allowPartial,
                ParentId = parent?.id,
                SelectedQuantity = 0
            };
        }

        public void SetFromParent(bool isSelected)
        {
            _isSelected = isSelected;
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    record ServiceTypeOption(string? Value, string Label);

    class TableOptionVm
    {
        TableOptionVm(int? id, string displayName, bool @override)
        {
            Id = id;
            DisplayName = displayName;
            Override = @override;
        }

        public int? Id { get; }
        public string DisplayName { get; }
        public bool Override { get; }

        public static TableOptionVm KeepCurrent() => new(null, "Mantener mesa actual", false);
        public static TableOptionVm NoTable() => new(null, "Sin mesa", true);
        public static TableOptionVm From(TableDTO dto)
        {
            var name = string.IsNullOrWhiteSpace(dto.name) ? $"Mesa {dto.id}" : dto.name;
            if (dto.seats > 0)
                name += $" · {dto.seats} lugares";
            if (!dto.isActive)
                name += " (inactiva)";
            return new TableOptionVm(dto.id, name, true);
        }
    }

    public class SplitPlanResult
    {
        public required OrderSplitRequestDto Request { get; init; }
        public required List<ItemSelection> Items { get; init; }
    }

    public class ItemSelection
    {
        public required int ItemId { get; init; }
        public required int OriginalQuantity { get; init; }
        public required int SelectedQuantity { get; init; }
        public bool AllowPartial { get; init; }
    }
}
