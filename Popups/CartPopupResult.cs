using Imdeliceapp.Pages;

namespace Imdeliceapp.Popups;

public enum CartPopupAction
{
    None,
    Checkout,
    EditLine
}

public record CartPopupResult(
    CartPopupAction Action,
    TakeOrderPage.CartEntry? LineToEdit = null,
    TakeOrderPage.OrderHeaderState? Header = null);
