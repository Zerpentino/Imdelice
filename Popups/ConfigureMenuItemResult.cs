using System;
using System.Collections.Generic;
using Imdeliceapp.Pages;

namespace Imdeliceapp.Popups;

public record ConfigureMenuItemResult(
    TakeOrderPage.MenuItemVm BaseItem,
    TakeOrderPage.MenuItemVm SelectedItem,
    int Quantity,
    string? Notes,
    IReadOnlyList<TakeOrderPage.CartModifierSelection> Modifiers,
    Guid? EditedLineId = null);
