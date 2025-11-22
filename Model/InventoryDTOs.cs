using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Imdeliceapp.Models;

public class InventoryLocationDTO
{
    public int id { get; set; }
    public string name { get; set; } = string.Empty;
    public string? code { get; set; }
    public string type { get; set; } = string.Empty;
    public bool isDefault { get; set; }
    public bool isActive { get; set; }
}

public class InventoryLocationCreateRequest
{
    public string name { get; set; } = string.Empty;
    public string? code { get; set; }
    public string type { get; set; } = "GENERAL";
    public bool isDefault { get; set; }
}

public class InventoryProductSnapshot
{
    public int id { get; set; }
    public string? name { get; set; }
    public string? sku { get; set; }
    public string? barcode { get; set; }
    public string? imageUrl { get; set; }
    public bool hasImage { get; set; }
    public string? categoryName { get; set; }
    public string? categorySlug { get; set; }
}

public class InventoryItemDTO
{
    public int id { get; set; }
    public int productId { get; set; }
    public InventoryProductSnapshot? product { get; set; }
    public int? variantId { get; set; }
    public InventoryProductSnapshot? variant { get; set; }
    public int locationId { get; set; }
    public InventoryLocationDTO? location { get; set; }
    [JsonConverter(typeof(FlexibleDecimalConverter))]
    public decimal currentQuantity { get; set; }
    public string unit { get; set; } = "UNIT";
    [JsonConverter(typeof(FlexibleNullableDecimalConverter))]
    public decimal? minThreshold { get; set; }
    [JsonConverter(typeof(FlexibleNullableDecimalConverter))]
    public decimal? maxThreshold { get; set; }
    public DateTime? lastMovementAt { get; set; }
}

public class InventoryMovementDTO
{
    public int id { get; set; }
    public int productId { get; set; }
    public InventoryProductSnapshot? product { get; set; }
    public int? variantId { get; set; }
    public InventoryProductSnapshot? variant { get; set; }
    public int? locationId { get; set; }
    public InventoryLocationDTO? location { get; set; }
    public string type { get; set; } = string.Empty;
    [JsonConverter(typeof(FlexibleDecimalConverter))]
    public decimal quantity { get; set; }
    public string unit { get; set; } = "UNIT";
    public string? reason { get; set; }
    public int? relatedOrderId { get; set; }
    public int? performedByUserId { get; set; }
    public DateTime createdAt { get; set; }
}

public class InventoryMovementRequest
{
    public int productId { get; set; }
    public int? variantId { get; set; }
    public int? locationId { get; set; }
    public string type { get; set; } = string.Empty;
    public decimal quantity { get; set; }
    public string? reason { get; set; }
}

public class InventoryMovementByBarcodeRequest
{
    public string barcode { get; set; } = string.Empty;
    public int? locationId { get; set; }
    public string type { get; set; } = string.Empty;
    public decimal quantity { get; set; }
    public string? reason { get; set; }
}
