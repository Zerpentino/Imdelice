using System;

namespace Imdeliceapp.Helpers;

public static class InventoryMovementHelper
{
    public static decimal NormalizeQuantity(string? type, decimal quantity)
    {
        var normalizedType = type?.ToUpperInvariant() ?? string.Empty;
        var absolute = Math.Abs(quantity);
        return normalizedType switch
        {
            "SALE" => -absolute,
            "WASTE" => -absolute,
            "SALE_RETURN" => absolute,
            "PURCHASE" => absolute,
            _ => quantity
        };
    }
}
