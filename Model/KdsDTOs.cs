using System;
using System.Collections.Generic;

namespace Imdeliceapp.Models;

public class KdsTicketDTO
{
    public int orderId { get; set; }
    public string code { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
    public string serviceType { get; set; } = string.Empty;
    public string source { get; set; } = string.Empty;
    public DateTime? openedAt { get; set; }
    public int? prepEtaMinutes { get; set; }
    public string? note { get; set; }
    public string? customerName { get; set; }
    public KdsTableDTO? table { get; set; }
    public List<KdsItemDTO> items { get; set; } = new();
}

public class KdsTableDTO
{
    public int id { get; set; }
    public string? name { get; set; }
}

public class KdsItemDTO
{
    public int id { get; set; }
    public int productId { get; set; }
    public int? parentItemId { get; set; }
    public string? name { get; set; }
    public string? variantName { get; set; }
    public int quantity { get; set; }
    public string status { get; set; } = string.Empty;
    public string? notes { get; set; }
    public bool isComboParent { get; set; }
    public List<KdsItemModifierDTO> modifiers { get; set; } = new();
    public List<KdsItemDTO> children { get; set; } = new();
}

public class KdsItemModifierDTO
{
    public string? name { get; set; }
    public int quantity { get; set; }
}

public class KdsQuery
{
    public IList<string>? Statuses { get; set; }
    public string? ServiceType { get; set; }
    public string? Source { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int? TzOffsetMinutes { get; set; }
}
