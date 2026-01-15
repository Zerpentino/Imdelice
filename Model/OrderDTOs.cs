using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Imdeliceapp.Models;

public class OrderSummaryDTO
{
    public int id { get; set; }
    public string code { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
    public string serviceType { get; set; } = string.Empty;
    public string source { get; set; } = string.Empty;
    public string? externalRef { get; set; }
    public int? tableId { get; set; }
    public int? prepEtaMinutes { get; set; }
    public DateTime? openedAt { get; set; }
    public DateTime? acceptedAt { get; set; }
    public DateTime? readyAt { get; set; }
    public DateTime? servedAt { get; set; }
    public DateTime? closedAt { get; set; }
    public DateTime? canceledAt { get; set; }
    public int subtotalCents { get; set; }
    public int discountCents { get; set; }
    public int serviceFeeCents { get; set; }
    public int deliveryFeeCents { get; set; }
    public int taxCents { get; set; }
    public int totalCents { get; set; }
    public int paymentsTotalCents { get; set; }
    public int paymentsTipCents { get; set; }
    public OrderTableDTO? table { get; set; }
    public List<OrderPaymentDTO> payments { get; set; } = new();
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? platformMarkupPct { get; set; }
    public OrderUserSnapshot? servedBy { get; set; }
}

public class OrderDetailDTO : OrderSummaryDTO
{
    public string? note { get; set; }
    public string? customerName { get; set; }
    public string? customerPhone { get; set; }
    public int? covers { get; set; }
    public int? servedByUserId { get; set; }
    public List<OrderItemDTO> items { get; set; } = new();
}

public class OrderTableDTO
{
    public int id { get; set; }
    public string name { get; set; } = string.Empty;
    public int? seats { get; set; }
    public bool? isActive { get; set; }
}

public class OrderPaymentDTO
{
    public int id { get; set; }
    public int orderId { get; set; }
    public string method { get; set; } = string.Empty;
    public int amountCents { get; set; }
    public int tipCents { get; set; }
    public DateTime? paidAt { get; set; }
    public int? receivedByUserId { get; set; }
    public string? note { get; set; }
}

public class OrderItemDTO
{
    public int id { get; set; }
    public int orderId { get; set; }
    public int productId { get; set; }
    public OrderItemProductSnapshot? product { get; set; }
    public int? variantId { get; set; }
    public OrderItemVariantSnapshot? variant { get; set; }
    public int? parentItemId { get; set; }
    public OrderItemParentRef? parentItem { get; set; }
    public List<OrderItemDTO> childItems { get; set; } = new();
    public int quantity { get; set; }
    public string status { get; set; } = string.Empty;
    public int basePriceCents { get; set; }
    public int extrasTotalCents { get; set; }
    public int totalPriceCents { get; set; }
    public string? nameSnapshot { get; set; }
    public string? variantNameSnapshot { get; set; }
    public string? notes { get; set; }
    public List<OrderItemModifierDTO> modifiers { get; set; } = new();
}

public class OrderItemParentRef
{
    public int id { get; set; }
}

public class OrderItemProductSnapshot
{
    public int id { get; set; }
    public string? name { get; set; }
    public string? type { get; set; }
}

public class OrderItemVariantSnapshot
{
    public int id { get; set; }
    public int productId { get; set; }
    public string? name { get; set; }
    public int? priceCents { get; set; }
}

public class OrderItemModifierDTO
{
    public int id { get; set; }
    public int orderItemId { get; set; }
    public int optionId { get; set; }
    public int quantity { get; set; }
    public int priceExtraCents { get; set; }
    public string? nameSnapshot { get; set; }
}

#region Snapshot DTOs

public class OrderUserSnapshot
{
    public int id { get; set; }
    public string? name { get; set; }
    public string? email { get; set; }
}

#endregion

#region Request DTOs

public class CreateOrderDTO
{
    public string serviceType { get; set; } = string.Empty;
    public string source { get; set; } = "POS";
    public int? deliveryFeeCents { get; set; }
    public string? status { get; set; }
    public decimal? platformMarkupPct { get; set; }
    public int? tableId { get; set; }
    public int? covers { get; set; }
    public string? note { get; set; }
    public string? customerName { get; set; }
    public string? customerPhone { get; set; }
    public string? externalRef { get; set; }
    public int? prepEtaMinutes { get; set; }
    public int? servedByUserId { get; set; }
    public List<CreateOrderItemDTO> items { get; set; } = new();
}

public class QuickOrderRequestDto
{
    public string? serviceType { get; set; }
    public int totalCents { get; set; }
    public List<QuickOrderItemDto> items { get; set; } = new();
}

public class QuickOrderItemDto
{
    public int productId { get; set; }
    public int? variantId { get; set; }
    public int quantity { get; set; }
}

public class CreateOrderItemDTO
{
    public int productId { get; set; }
    public int? variantId { get; set; }
    public int? quantity { get; set; }
    public string? notes { get; set; }
    public List<OrderModifierSelectionInput> modifiers { get; set; } = new();
    public List<ComboChildSelectionInput>? children { get; set; }
}

public class OrderModifierSelectionInput
{
    public int optionId { get; set; }
    public int? quantity { get; set; }
}

public class AddOrderItemDto
{
    public int? orderId { get; set; } // opcional cuando se reutiliza para POST /orders al crear
    public int productId { get; set; }
    public int? variantId { get; set; }
    public int? quantity { get; set; }
    public string? notes { get; set; }
    public List<OrderModifierSelectionInput> modifiers { get; set; } = new();
    public List<ComboChildSelectionInput>? children { get; set; }
}

public class UpdateOrderItemDto
{
    public int? quantity { get; set; }
    public string? notes { get; set; }
    public List<OrderModifierSelectionInput>? replaceModifiers { get; set; }
}

public class UpdateOrderItemStatusDto
{
    public string status { get; set; } = string.Empty;
    public string? reason { get; set; }
}

public class UpdateOrderMetaDto
{
    public int? tableId { get; set; }
    public int? covers { get; set; }
    public string? note { get; set; }
    public int? prepEtaMinutes { get; set; }
    public int? deliveryFeeCents { get; set; }
}

public class UpdateOrderStatusDto
{
    public string status { get; set; } = string.Empty;
    public string? reason { get; set; }
}

public class RefundOrderRequest
{
    public string? reason { get; set; }
    public string? adminEmail { get; set; }
    public string? adminPin { get; set; }
    public string password { get; set; } = string.Empty;
}

public class RefundOrderResponse
{
    public int orderId { get; set; }
    public string status { get; set; } = string.Empty;
    public DateTime? refundedAt { get; set; }
}

public class AddPaymentDto
{
    public string method { get; set; } = "CASH";
    public int amountCents { get; set; }
    public int tipCents { get; set; }
    public string? note { get; set; }
    public int? receivedAmountCents { get; set; }
    public int? changeCents { get; set; }
}

public class ComboChildSelectionInput
{
    public int productId { get; set; }
    public int? variantId { get; set; }
    public int quantity { get; set; }
    public string? notes { get; set; }
    public List<OrderModifierSelectionInput>? modifiers { get; set; }
}

public class ChannelConfigDTO
{
    public string source { get; set; } = string.Empty;
    public int markupPct { get; set; }
    public DateTime? updatedAt { get; set; }
}

public class ChannelConfigUpdateDto
{
    public int markupPct { get; set; }
}

public class PaymentsReportDto
{
    public PaymentsReportRangeDto range { get; set; } = new();
    public List<PaymentsReportMethodSummaryDto> totalsByMethod { get; set; } = new();
    public PaymentsReportTotalsDto grandTotals { get; set; } = new();
    public List<PaymentsReportOrderDto> orders { get; set; } = new();
}

public class PaymentsReportRangeDto
{
    public DateTime? from { get; set; }
    public DateTime? to { get; set; }
}

public class PaymentsReportMethodSummaryDto
{
    public string method { get; set; } = string.Empty;
    public int paymentsCount { get; set; }
    public int amountCents { get; set; }
    public int tipCents { get; set; }
    public int changeCents { get; set; }
    public int receivedAmountCents { get; set; }
}

public class PaymentsReportTotalsDto
{
    public int paymentsCount { get; set; }
    public int amountCents { get; set; }
    public int tipCents { get; set; }
    public int changeCents { get; set; }
    public int receivedAmountCents { get; set; }
    public int ordersClosed { get; set; }
    public int ordersCanceled { get; set; }
    public int ordersRefunded { get; set; }
}

public class PaymentsReportOrderDto
{
    public int id { get; set; }
    public int? orderId { get; set; }
    public string code { get; set; } = string.Empty;
    public string serviceType { get; set; } = string.Empty;
    public string? source { get; set; }
    public string status { get; set; } = string.Empty;
    public DateTime? openedAt { get; set; }
    public DateTime? closedAt { get; set; }
    public DateTime? canceledAt { get; set; }
    public DateTime? refundedAt { get; set; }
    public OrderTableDTO? table { get; set; }
    public string? note { get; set; }
    public int subtotalCents { get; set; }
    public int deliveryFeeCents { get; set; }
    public int totalCents { get; set; }
    public List<PaymentsReportOrderPaymentDto> payments { get; set; } = new();
}

public class PaymentsReportOrderPaymentDto
{
    public int id { get; set; }
    public string method { get; set; } = string.Empty;
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int amountCents { get; set; }
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int tipCents { get; set; }
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int changeCents { get; set; }
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int receivedAmountCents { get; set; }
    public DateTime? paidAt { get; set; }
}

public class OrderSplitRequestDto
{
    public List<int> itemIds { get; set; } = new();
    public string? serviceType { get; set; }
    public int? tableId { get; set; }
    [JsonIgnore]
    public bool SendTableId { get; set; }
    public string? note { get; set; }
    public int? covers { get; set; }
    public int? deliveryFeeCents { get; set; }
}

public class OrderSplitResponseDto
{
    public int newOrderId { get; set; }
    public string code { get; set; } = string.Empty;
}

public class OrderListQuery
{
    public string? statuses { get; set; }
    public string? serviceType { get; set; }
    public string? source { get; set; }
    public DateTime? from { get; set; }
    public DateTime? to { get; set; }
    public int? tableId { get; set; }
    public int? tzOffsetMinutes { get; set; }
}

public class FlexibleIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                if (reader.TryGetInt32(out var intValue))
                    return intValue;
                if (reader.TryGetDouble(out var doubleValue))
                    return (int)Math.Round(doubleValue, MidpointRounding.AwayFromZero);
                break;
            case JsonTokenType.String:
                var str = reader.GetString();
                if (string.IsNullOrWhiteSpace(str))
                    return 0;
                if (int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt))
                    return parsedInt;
                if (decimal.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedDecimal))
                    return (int)Math.Round(parsedDecimal, MidpointRounding.AwayFromZero);
                break;
            case JsonTokenType.Null:
                return 0;
        }

        throw new JsonException($"Value \"{reader.GetString()}\" cannot be converted to Int32.");
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}

#endregion
