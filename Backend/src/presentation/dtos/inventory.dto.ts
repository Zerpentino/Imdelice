import { InventoryMovementType } from "@prisma/client";
import { z } from "zod";

const nonZeroQuantity = z
  .coerce
  .number()
  .refine((val) => val !== 0, { message: "quantity must be non-zero" });

export const InventoryMovementDto = z.object({
  productId: z.number().int().positive(),
  variantId: z.number().int().positive().nullable().optional(),
  locationId: z.number().int().positive().optional(),
  type: z.nativeEnum(InventoryMovementType),
  quantity: nonZeroQuantity,
  reason: z.string().max(255).optional(),
});

export const InventoryMovementByBarcodeDto = z.object({
  barcode: z.string().trim().min(3),
  locationId: z.number().int().positive().optional(),
  type: z.nativeEnum(InventoryMovementType),
  quantity: nonZeroQuantity,
  reason: z.string().max(255).optional(),
});

export const ListInventoryItemsQueryDto = z.object({
  id: z.coerce.number().int().positive().optional(),
  productId: z.coerce.number().int().positive().optional(),
  variantId: z.coerce.number().int().positive().optional(),
  locationId: z.coerce.number().int().positive().optional(),
  categoryId: z.coerce.number().int().positive().optional(),
  search: z.string().optional(),
  trackInventory: z.coerce.boolean().optional(),
});

export const InventoryMovementsQueryDto = z.object({
  productId: z.coerce.number().int().positive().optional(),
  variantId: z.coerce.number().int().positive().optional(),
  locationId: z.coerce.number().int().positive().optional(),
  orderId: z.coerce.number().int().positive().optional(),
  type: z.nativeEnum(InventoryMovementType).optional(),
  limit: z.coerce.number().int().positive().max(200).optional(),
});
