import { ExpenseCategory, PaymentMethod, InventoryMovementType } from "@prisma/client";
import { z } from "zod";

const inventoryMovementInput = z.object({
  productId: z.number().int().positive().optional(),
  variantId: z.number().int().positive().nullable().optional(),
  locationId: z.number().int().positive().optional(),
  barcode: z.string().trim().min(3).optional(),
  type: z.nativeEnum(InventoryMovementType).default("PURCHASE"),
  quantity: z.number().positive(),
  reason: z.string().optional(),
});

export const CreateExpenseDto = z.object({
  concept: z.string().min(3),
  category: z.nativeEnum(ExpenseCategory),
  amountCents: z.number().int().nonnegative(),
  paymentMethod: z.nativeEnum(PaymentMethod).nullable().optional(),
  notes: z.string().optional(),
  incurredAt: z.string().optional(),
  inventoryMovement: inventoryMovementInput.optional(),
  metadata: z.record(z.string(), z.any()).optional(),
});

export const UpdateExpenseDto = z.object({
  concept: z.string().min(3).optional(),
  category: z.nativeEnum(ExpenseCategory).optional(),
  amountCents: z.number().int().nonnegative().optional(),
  paymentMethod: z.nativeEnum(PaymentMethod).nullable().optional(),
  notes: z.string().optional(),
  incurredAt: z.string().optional(),
});

export const ExpenseQueryDto = z.object({
  from: z.string().optional(),
  to: z.string().optional(),
  category: z.nativeEnum(ExpenseCategory).optional(),
  recordedByUserId: z.coerce.number().int().positive().optional(),
  search: z.string().optional(),
  tzOffsetMinutes: z.coerce.number().optional(),
});

export const ExpensesSummaryQueryDto = ExpenseQueryDto;

export type InventoryMovementInputDto = z.infer<typeof inventoryMovementInput>;
