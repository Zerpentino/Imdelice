"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListOrdersQueryDto = exports.UpdateOrderStatusDto = exports.UpdateOrderMetaDto = exports.SplitOrderByItemsDto = exports.UpdateOrderItemDto = exports.AddPaymentDto = exports.UpdateOrderItemStatusDto = exports.CreateOrderDto = exports.AddOrderItemDto = exports.PaymentMethodEnum = exports.OrderItemStatusEnum = exports.OrderSourceEnum = exports.DraftOrderStatusEnum = exports.OrderStatusEnum = exports.ServiceTypeEnum = void 0;
const zod_1 = require("zod");
const serviceTypeValues = ["DINE_IN", "TAKEAWAY", "DELIVERY"];
const orderStatusValues = ["DRAFT", "OPEN", "HOLD", "CLOSED", "CANCELED", "REFUNDED"];
const draftableOrderStatusValues = ["DRAFT", "OPEN", "HOLD"];
const orderSourceValues = ["POS", "UBER", "DIDI", "RAPPI"];
exports.ServiceTypeEnum = zod_1.z.enum(serviceTypeValues);
exports.OrderStatusEnum = zod_1.z.enum(orderStatusValues);
exports.DraftOrderStatusEnum = zod_1.z.enum(draftableOrderStatusValues);
exports.OrderSourceEnum = zod_1.z.enum(orderSourceValues);
exports.OrderItemStatusEnum = zod_1.z.enum(["NEW", "IN_PROGRESS", "READY", "SERVED", "CANCELED"]);
exports.PaymentMethodEnum = zod_1.z.enum(["CASH", "CARD", "TRANSFER", "OTHER"]);
exports.AddOrderItemDto = zod_1.z.lazy(() => zod_1.z.object({
    productId: zod_1.z.number().int().positive(),
    variantId: zod_1.z.number().int().positive().optional(),
    quantity: zod_1.z.number().int().positive().default(1),
    notes: zod_1.z.string().optional(),
    modifiers: zod_1.z.array(zod_1.z.object({
        optionId: zod_1.z.number().int().positive(),
        quantity: zod_1.z.number().int().positive().default(1),
    })).default([]),
    children: zod_1.z.array(exports.AddOrderItemDto).optional()
}));
exports.CreateOrderDto = zod_1.z.object({
    serviceType: exports.ServiceTypeEnum,
    source: exports.OrderSourceEnum.optional(),
    tableId: zod_1.z.number().int().positive().optional(),
    note: zod_1.z.string().optional(),
    covers: zod_1.z.number().int().positive().optional(),
    externalRef: zod_1.z.string().min(1).max(191).optional(),
    prepEtaMinutes: zod_1.z.number().int().nonnegative().optional(),
    status: exports.DraftOrderStatusEnum.optional(), // default OPEN
    platformMarkupPct: zod_1.z.number().finite().min(0).max(300).optional(),
    items: zod_1.z.array(exports.AddOrderItemDto).optional(),
});
exports.UpdateOrderItemStatusDto = zod_1.z.object({
    status: exports.OrderItemStatusEnum
});
exports.AddPaymentDto = zod_1.z.object({
    method: exports.PaymentMethodEnum,
    amountCents: zod_1.z.number().int().nonnegative(),
    tipCents: zod_1.z.number().int().nonnegative().default(0),
    receivedAmountCents: zod_1.z.number().int().nonnegative().optional(),
    changeCents: zod_1.z.number().int().nonnegative().optional(),
    note: zod_1.z.string().optional()
});
exports.UpdateOrderItemDto = zod_1.z.object({
    quantity: zod_1.z.number().int().positive().optional(),
    notes: zod_1.z.string().optional(),
    // Reemplaza por completo los modificadores del renglón (opcional)
    replaceModifiers: zod_1.z.array(zod_1.z.object({
        optionId: zod_1.z.number().int().positive(),
        quantity: zod_1.z.number().int().positive().default(1)
    })).optional()
});
exports.SplitOrderByItemsDto = zod_1.z.object({
    itemIds: zod_1.z.array(zod_1.z.number().int().positive()).min(1),
    // Puedes sobreescribir metadatos del pedido nuevo (opcionales)
    serviceType: exports.ServiceTypeEnum.optional(),
    tableId: zod_1.z.number().int().positive().nullable().optional(),
    note: zod_1.z.string().optional(),
    covers: zod_1.z.number().int().positive().optional()
});
exports.UpdateOrderMetaDto = zod_1.z.object({
    tableId: zod_1.z.number().int().positive().nullable().optional(), // null para quitar mesa
    note: zod_1.z.string().nullable().optional(),
    covers: zod_1.z.number().int().positive().nullable().optional(),
    prepEtaMinutes: zod_1.z.number().int().nonnegative().nullable().optional()
});
exports.UpdateOrderStatusDto = zod_1.z.object({
    status: exports.OrderStatusEnum
});
const StatusesParamSchema = zod_1.z
    .union([
    zod_1.z.string(),
    zod_1.z.array(zod_1.z.string())
])
    .optional()
    .transform((value) => {
    if (!value)
        return undefined;
    const rawList = Array.isArray(value) ? value : value.split(",");
    const sanitized = rawList
        .map((s) => s.trim())
        .filter(Boolean)
        .map((s) => exports.OrderStatusEnum.parse(s));
    return sanitized.length ? Array.from(new Set(sanitized)) : undefined;
});
exports.ListOrdersQueryDto = zod_1.z.object({
    statuses: StatusesParamSchema,
    serviceType: zod_1.z
        .union([exports.ServiceTypeEnum, zod_1.z.array(exports.ServiceTypeEnum)])
        .optional()
        .transform((value) => {
        if (!value)
            return undefined;
        const values = Array.isArray(value) ? value : [value];
        return values.length ? values[0] : undefined;
    }),
    source: zod_1.z
        .union([exports.OrderSourceEnum, zod_1.z.array(exports.OrderSourceEnum)])
        .optional()
        .transform((value) => {
        if (!value)
            return undefined;
        const values = Array.isArray(value) ? value : [value];
        return values.length ? values[0] : undefined;
    }),
    from: zod_1.z.string().optional(),
    to: zod_1.z.string().optional(),
    tableId: zod_1.z.coerce.number().int().positive().optional(),
    search: zod_1.z.string().optional()
});
//# sourceMappingURL=orders.dto.js.map