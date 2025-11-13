import { z } from "zod";
export declare const ServiceTypeEnum: z.ZodEnum<{
    DINE_IN: "DINE_IN";
    TAKEAWAY: "TAKEAWAY";
    DELIVERY: "DELIVERY";
}>;
export declare const OrderStatusEnum: z.ZodEnum<{
    DRAFT: "DRAFT";
    OPEN: "OPEN";
    HOLD: "HOLD";
    CLOSED: "CLOSED";
    CANCELED: "CANCELED";
    REFUNDED: "REFUNDED";
}>;
export declare const DraftOrderStatusEnum: z.ZodEnum<{
    DRAFT: "DRAFT";
    OPEN: "OPEN";
    HOLD: "HOLD";
}>;
export declare const OrderSourceEnum: z.ZodEnum<{
    POS: "POS";
    UBER: "UBER";
    DIDI: "DIDI";
    RAPPI: "RAPPI";
}>;
export declare const OrderItemStatusEnum: z.ZodEnum<{
    CANCELED: "CANCELED";
    NEW: "NEW";
    IN_PROGRESS: "IN_PROGRESS";
    READY: "READY";
    SERVED: "SERVED";
}>;
export declare const PaymentMethodEnum: z.ZodEnum<{
    CASH: "CASH";
    CARD: "CARD";
    TRANSFER: "TRANSFER";
    OTHER: "OTHER";
}>;
export declare const AddOrderItemDto: z.ZodType<any>;
export type AddOrderItemDtoType = z.infer<typeof AddOrderItemDto>;
export declare const CreateOrderDto: z.ZodObject<{
    serviceType: z.ZodEnum<{
        DINE_IN: "DINE_IN";
        TAKEAWAY: "TAKEAWAY";
        DELIVERY: "DELIVERY";
    }>;
    source: z.ZodOptional<z.ZodEnum<{
        POS: "POS";
        UBER: "UBER";
        DIDI: "DIDI";
        RAPPI: "RAPPI";
    }>>;
    tableId: z.ZodOptional<z.ZodNumber>;
    note: z.ZodOptional<z.ZodString>;
    covers: z.ZodOptional<z.ZodNumber>;
    externalRef: z.ZodOptional<z.ZodString>;
    prepEtaMinutes: z.ZodOptional<z.ZodNumber>;
    status: z.ZodOptional<z.ZodEnum<{
        DRAFT: "DRAFT";
        OPEN: "OPEN";
        HOLD: "HOLD";
    }>>;
    platformMarkupPct: z.ZodOptional<z.ZodNumber>;
    items: z.ZodOptional<z.ZodArray<z.ZodType<any, unknown, z.core.$ZodTypeInternals<any, unknown>>>>;
}, z.core.$strip>;
export type CreateOrderDtoType = z.infer<typeof CreateOrderDto>;
export declare const UpdateOrderItemStatusDto: z.ZodObject<{
    status: z.ZodEnum<{
        CANCELED: "CANCELED";
        NEW: "NEW";
        IN_PROGRESS: "IN_PROGRESS";
        READY: "READY";
        SERVED: "SERVED";
    }>;
}, z.core.$strip>;
export declare const AddPaymentDto: z.ZodObject<{
    method: z.ZodEnum<{
        CASH: "CASH";
        CARD: "CARD";
        TRANSFER: "TRANSFER";
        OTHER: "OTHER";
    }>;
    amountCents: z.ZodNumber;
    tipCents: z.ZodDefault<z.ZodNumber>;
    receivedAmountCents: z.ZodOptional<z.ZodNumber>;
    changeCents: z.ZodOptional<z.ZodNumber>;
    note: z.ZodOptional<z.ZodString>;
}, z.core.$strip>;
export declare const UpdateOrderItemDto: z.ZodObject<{
    quantity: z.ZodOptional<z.ZodNumber>;
    notes: z.ZodOptional<z.ZodString>;
    replaceModifiers: z.ZodOptional<z.ZodArray<z.ZodObject<{
        optionId: z.ZodNumber;
        quantity: z.ZodDefault<z.ZodNumber>;
    }, z.core.$strip>>>;
}, z.core.$strip>;
export type UpdateOrderItemDtoType = z.infer<typeof UpdateOrderItemDto>;
export declare const SplitOrderByItemsDto: z.ZodObject<{
    itemIds: z.ZodArray<z.ZodNumber>;
    serviceType: z.ZodOptional<z.ZodEnum<{
        DINE_IN: "DINE_IN";
        TAKEAWAY: "TAKEAWAY";
        DELIVERY: "DELIVERY";
    }>>;
    tableId: z.ZodOptional<z.ZodNullable<z.ZodNumber>>;
    note: z.ZodOptional<z.ZodString>;
    covers: z.ZodOptional<z.ZodNumber>;
}, z.core.$strip>;
export type SplitOrderByItemsDtoType = z.infer<typeof SplitOrderByItemsDto>;
export declare const UpdateOrderMetaDto: z.ZodObject<{
    tableId: z.ZodOptional<z.ZodNullable<z.ZodNumber>>;
    note: z.ZodOptional<z.ZodNullable<z.ZodString>>;
    covers: z.ZodOptional<z.ZodNullable<z.ZodNumber>>;
    prepEtaMinutes: z.ZodOptional<z.ZodNullable<z.ZodNumber>>;
}, z.core.$strip>;
export type UpdateOrderMetaDtoType = z.infer<typeof UpdateOrderMetaDto>;
export declare const UpdateOrderStatusDto: z.ZodObject<{
    status: z.ZodEnum<{
        DRAFT: "DRAFT";
        OPEN: "OPEN";
        HOLD: "HOLD";
        CLOSED: "CLOSED";
        CANCELED: "CANCELED";
        REFUNDED: "REFUNDED";
    }>;
}, z.core.$strip>;
export type UpdateOrderStatusDtoType = z.infer<typeof UpdateOrderStatusDto>;
export declare const ListOrdersQueryDto: z.ZodObject<{
    statuses: z.ZodPipe<z.ZodOptional<z.ZodUnion<readonly [z.ZodString, z.ZodArray<z.ZodString>]>>, z.ZodTransform<("DRAFT" | "OPEN" | "HOLD" | "CLOSED" | "CANCELED" | "REFUNDED")[] | undefined, string | string[] | undefined>>;
    serviceType: z.ZodPipe<z.ZodOptional<z.ZodUnion<readonly [z.ZodEnum<{
        DINE_IN: "DINE_IN";
        TAKEAWAY: "TAKEAWAY";
        DELIVERY: "DELIVERY";
    }>, z.ZodArray<z.ZodEnum<{
        DINE_IN: "DINE_IN";
        TAKEAWAY: "TAKEAWAY";
        DELIVERY: "DELIVERY";
    }>>]>>, z.ZodTransform<"DINE_IN" | "TAKEAWAY" | "DELIVERY" | undefined, "DINE_IN" | "TAKEAWAY" | "DELIVERY" | ("DINE_IN" | "TAKEAWAY" | "DELIVERY")[] | undefined>>;
    source: z.ZodPipe<z.ZodOptional<z.ZodUnion<readonly [z.ZodEnum<{
        POS: "POS";
        UBER: "UBER";
        DIDI: "DIDI";
        RAPPI: "RAPPI";
    }>, z.ZodArray<z.ZodEnum<{
        POS: "POS";
        UBER: "UBER";
        DIDI: "DIDI";
        RAPPI: "RAPPI";
    }>>]>>, z.ZodTransform<"POS" | "UBER" | "DIDI" | "RAPPI" | undefined, "POS" | "UBER" | "DIDI" | "RAPPI" | ("POS" | "UBER" | "DIDI" | "RAPPI")[] | undefined>>;
    from: z.ZodOptional<z.ZodString>;
    to: z.ZodOptional<z.ZodString>;
    tableId: z.ZodOptional<z.ZodCoercedNumber<unknown>>;
    search: z.ZodOptional<z.ZodString>;
}, z.core.$strip>;
export type ListOrdersQueryDtoType = z.infer<typeof ListOrdersQueryDto>;
//# sourceMappingURL=orders.dto.d.ts.map