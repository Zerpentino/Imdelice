using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Maui.Views;
using Imdeliceapp.Models;
using Microsoft.Maui.Controls;

namespace Imdeliceapp.Popups;

public partial class OrderItemPickerPopup : Popup
{
    readonly ViewModel _viewModel;

    public OrderItemPickerPopup(IEnumerable<OrderItemDTO> items, string actionDescription)
    {
        InitializeComponent();
        _viewModel = new ViewModel(items, actionDescription);
        BindingContext = _viewModel;
    }

    void CloseButton_Clicked(object sender, EventArgs e) => Close(null);

    void SelectButton_Clicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is OrderItemOptionVm vm)
            Close(vm.Source);
    }

    void Items_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is OrderItemOptionVm vm)
            Close(vm.Source);
    }

    class ViewModel
    {
        public ViewModel(IEnumerable<OrderItemDTO> items, string actionDescription)
        {
            Title = "Selecciona el producto";
            Subtitle = $"Elige qué producto deseas {actionDescription}.";
            Items = new ObservableCollection<OrderItemOptionVm>(Flatten(items));
        }

        public string Title { get; }
        public string Subtitle { get; }
        public ObservableCollection<OrderItemOptionVm> Items { get; }

        static IEnumerable<OrderItemOptionVm> Flatten(IEnumerable<OrderItemDTO> items)
        {
            foreach (var item in items)
            {
                foreach (var flattened in FlattenInternal(item, 0, null))
                    yield return flattened;
            }
        }

        static IEnumerable<OrderItemOptionVm> FlattenInternal(OrderItemDTO item, int level, OrderItemDTO? parent, int? occurrence = null, int? total = null)
        {
            yield return OrderItemOptionVm.From(item, level, parent, occurrence, total);

            if (item.childItems == null)
                yield break;

            var groups = item.childItems
                .GroupBy(c => c.nameSnapshot ?? c.product?.name ?? $"Producto {c.productId}")
                .ToList();

            foreach (var group in groups)
            {
                var index = 0;
                var groupCount = group.Count();
                foreach (var child in group)
                {
                    index++;
                    foreach (var flattened in FlattenInternal(child, level + 1, item, groupCount > 1 ? index : (int?)null, groupCount > 1 ? groupCount : (int?)null))
                        yield return flattened;
                }
            }
        }
    }

    public class OrderItemOptionVm
    {
        public OrderItemDTO Source { get; init; } = null!;
        public string DisplayTitle { get; init; } = string.Empty;
        public string Detail { get; init; } = string.Empty;
        public Thickness Margin { get; init; } = new(0);

        public static OrderItemOptionVm From(OrderItemDTO item, int level, OrderItemDTO? parent, int? occurrence, int? groupSize)
        {
            var detailParts = new List<string>
            {
                $"{item.quantity}×"
            };

            if (!string.IsNullOrWhiteSpace(item.status))
                detailParts.Add(item.status);

            if (!string.IsNullOrWhiteSpace(item.notes))
                detailParts.Add($"Nota: {item.notes}");

            if (parent != null)
            {
                var parentName = parent.nameSnapshot ?? parent.product?.name ?? $"Combo #{parent.id}";
                detailParts.Add($"Combo: {parentName}");
                if (occurrence.HasValue && groupSize.HasValue)
                    detailParts.Add($"Elemento {occurrence}/{groupSize}");
            }

            return new OrderItemOptionVm
            {
                Source = item,
                DisplayTitle = parent == null
                    ? item.nameSnapshot ?? item.product?.name ?? $"Producto {item.productId}"
                    : $"{item.nameSnapshot ?? item.product?.name ?? $"Producto {item.productId}"}",
                Detail = string.Join(" · ", detailParts),
                Margin = new Thickness(level * 12, 6, 0, 6)
            };
        }
    }
}
