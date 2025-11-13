using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Maui.Views;
using Imdeliceapp.Models;
using Microsoft.Maui.Controls;

namespace Imdeliceapp.Popups;

public partial class OrderItemsManagerPopup : Popup
{
    public enum ItemAction
    {
        Edit,
        ChangeStatus,
        Delete,
        AddProduct
    }

    public new record Result(ItemAction Action, OrderItemDTO? Item);

    public ObservableCollection<OrderItemEntryVm> Items { get; } = new();

    public OrderItemsManagerPopup(IEnumerable<OrderItemDTO> rootItems)
    {
        InitializeComponent();
        BindingContext = this;
        BuildItems(rootItems ?? Array.Empty<OrderItemDTO>());
    }

    void BuildItems(IEnumerable<OrderItemDTO> rootItems)
    {
        Items.Clear();
        foreach (var item in rootItems)
        {
            foreach (var entry in Flatten(item, 0, item.parentItemId.HasValue ? item.parentItemId.Value : (int?)null))
                Items.Add(entry);
        }
    }

    static IEnumerable<OrderItemEntryVm> Flatten(OrderItemDTO item, int level, int? parentId)
    {
        yield return OrderItemEntryVm.From(item, level, parentId);

        if (item.childItems == null)
            yield break;

        foreach (var child in item.childItems)
        {
            foreach (var flattened in Flatten(child, level + 1, item.id))
                yield return flattened;
        }
    }

    void CloseButton_Clicked(object sender, EventArgs e) => Close(null);

    void AddProduct_Clicked(object sender, EventArgs e) => Close(new Result(ItemAction.AddProduct, null));

    void EditButton_Clicked(object sender, EventArgs e) => CloseWithAction(sender, ItemAction.Edit);

    void StatusButton_Clicked(object sender, EventArgs e) => CloseWithAction(sender, ItemAction.ChangeStatus);

    void DeleteButton_Clicked(object sender, EventArgs e) => CloseWithAction(sender, ItemAction.Delete);

    void CloseWithAction(object sender, ItemAction action)
    {
        if ((sender as Button)?.CommandParameter is not OrderItemEntryVm vm)
            return;
        Close(new Result(action, vm.Source));
    }

    public class OrderItemEntryVm
    {
        public required OrderItemDTO Source { get; init; }
        public required string DisplayTitle { get; init; }
        public required string Detail { get; init; }
        public required Thickness Margin { get; init; }

        public static OrderItemEntryVm From(OrderItemDTO item, int level, int? parentId)
        {
            var detailParts = new List<string>
            {
                $"{item.quantity}×"
            };

            if (!string.IsNullOrWhiteSpace(item.status))
                detailParts.Add(item.status);
            if (!string.IsNullOrWhiteSpace(item.notes))
                detailParts.Add($"Nota: {item.notes}");
            if (parentId.HasValue)
                detailParts.Add($"Combo #{parentId.Value}");

            return new OrderItemEntryVm
            {
                Source = item,
                DisplayTitle = item.nameSnapshot ?? item.product?.name ?? $"Producto {item.productId}",
                Detail = string.Join(" · ", detailParts),
                Margin = new Thickness(level * 16, 6, 0, 6)
            };
        }
    }
}
