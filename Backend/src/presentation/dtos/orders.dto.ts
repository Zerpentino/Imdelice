import { z } from "zod";

const serviceTypeValues = ["DINE_IN", "TAKEAWAY", "DELIVERY"] as const;
const orderStatusValues = ["DRAFT", "OPEN", "HOLD", "CLOSED", "CANCELED", "REFUNDED"] as const;
const draftableOrderStatusValues = ["DRAFT", "OPEN", "HOLD"] as const;
const orderSourceValues = ["POS", "UBER", "DIDI", "RAPPI"] as const;

export const ServiceTypeEnum = z.enum(serviceTypeValues);
export const OrderStatusEnum = z.enum(orderStatusValues);
export const DraftOrderStatusEnum = z.enum(draftableOrderStatusValues);
export const OrderSourceEnum = z.enum(orderSourceValues);
export const OrderItemStatusEnum = z.enum(["NEW", "IN_PROGRESS", "READY", "SERVED", "CANCELED"]);
export const PaymentMethodEnum = z.enum(["CASH", "CARD", "TRANSFER", "OTHER"]);

export const AddOrderItemDto: z.ZodType<any> = z.lazy(() =>
  z.object({
    productId: z.number().int().positive(),
    variantId: z.number().int().positive().optional(),
    quantity: z.number().int().positive().default(1),
    notes: z.string().optional(),
    modifiers: z.array(z.object({
      optionId: z.number().int().positive(),
      quantity: z.number().int().positive().default(1),
    })).default([]),
    children: z.array(AddOrderItemDto).optional()
  })
);
export type AddOrderItemDtoType = z.infer<typeof AddOrderItemDto>;

export const CreateOrderDto = z.object({
  serviceType: ServiceTypeEnum,
  source: OrderSourceEnum.optional(),
  tableId: z.number().int().positive().optional(),
  note: z.string().optional(),
  covers: z.number().int().positive().optional(),
  externalRef: z.string().min(1).max(191).optional(),
  prepEtaMinutes: z.number().int().nonnegative().optional(),
  status: DraftOrderStatusEnum.optional(), // default OPEN
  platformMarkupPct: z.number().finite().min(0).max(300).optional(),
  items: z.array(AddOrderItemDto).optional(),
});
export type CreateOrderDtoType = z.infer<typeof CreateOrderDto>;

export const UpdateOrderItemStatusDto = z.object({
  status: OrderItemStatusEnum
});

export const AddPaymentDto = z.object({
  method: PaymentMethodEnum,
  amountCents: z.number().int().nonnegative(),
  tipCents: z.number().int().nonnegative().default(0),
  receivedAmountCents: z.number().int().nonnegative().optional(),
  changeCents: z.number().int().nonnegative().optional(),
  note: z.string().optional()
});
export const UpdateOrderItemDto = z.object({
  quantity: z.number().int().positive().optional(),
  notes: z.string().optional(),
  // Reemplaza por completo los modificadores del renglón (opcional)
  replaceModifiers: z.array(z.object({
    optionId: z.number().int().positive(),
    quantity: z.number().int().positive().default(1)
  })).optional()
});
export type UpdateOrderItemDtoType = z.infer<typeof UpdateOrderItemDto>;

export const SplitOrderByItemsDto = z.object({
  itemIds: z.array(z.number().int().positive()).min(1),
  // Puedes sobreescribir metadatos del pedido nuevo (opcionales)
  serviceType: ServiceTypeEnum.optional(),
  tableId: z.number().int().positive().nullable().optional(),
  note: z.string().optional(),
  covers: z.number().int().positive().optional()
});
export type SplitOrderByItemsDtoType = z.infer<typeof SplitOrderByItemsDto>;
export const UpdateOrderMetaDto = z.object({
  tableId: z.number().int().positive().nullable().optional(), // null para quitar mesa
  note: z.string().nullable().optional(),
  covers: z.number().int().positive().nullable().optional(),
  prepEtaMinutes: z.number().int().nonnegative().nullable().optional()
});
export type UpdateOrderMetaDtoType = z.infer<typeof UpdateOrderMetaDto>;

export const UpdateOrderStatusDto = z.object({
  status: OrderStatusEnum
});
export type UpdateOrderStatusDtoType = z.infer<typeof UpdateOrderStatusDto>;

const StatusesParamSchema = z
  .union([
    z.string(),
    z.array(z.string())
  ])
  .optional()
  .transform((value) => {
    if (!value) return undefined;
    const rawList = Array.isArray(value) ? value : value.split(",");
    const sanitized = rawList
      .map((s) => s.trim())
      .filter(Boolean)
      .map((s) => OrderStatusEnum.parse(s));
    return sanitized.length ? Array.from(new Set(sanitized)) : undefined;
  });

export const ListOrdersQueryDto = z.object({
  statuses: StatusesParamSchema,
  serviceType: z
    .union([ServiceTypeEnum, z.array(ServiceTypeEnum)])
    .optional()
    .transform((value) => {
      if (!value) return undefined;
      const values = Array.isArray(value) ? value : [value];
      return values.length ? values[0] : undefined;
    }),
  source: z
    .union([OrderSourceEnum, z.array(OrderSourceEnum)])
    .optional()
    .transform((value) => {
      if (!value) return undefined;
      const values = Array.isArray(value) ? value : [value];
      return values.length ? values[0] : undefined;
    }),
  from: z.string().optional(),
  to: z.string().optional(),
  tableId: z.coerce.number().int().positive().optional(),
  search: z.string().optional()
});
export type ListOrdersQueryDtoType = z.infer<typeof ListOrdersQueryDto>;
