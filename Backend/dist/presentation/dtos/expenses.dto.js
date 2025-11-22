"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ExpensesSummaryQueryDto = exports.ExpenseQueryDto = exports.UpdateExpenseDto = exports.CreateExpenseDto = void 0;
const client_1 = require("@prisma/client");
const zod_1 = require("zod");
const inventoryMovementInput = zod_1.z.object({
    productId: zod_1.z.number().int().positive().optional(),
    variantId: zod_1.z.number().int().positive().nullable().optional(),
    locationId: zod_1.z.number().int().positive().optional(),
    barcode: zod_1.z.string().trim().min(3).optional(),
    type: zod_1.z.nativeEnum(client_1.InventoryMovementType).default("PURCHASE"),
    quantity: zod_1.z.number().positive(),
    reason: zod_1.z.string().optional(),
});
exports.CreateExpenseDto = zod_1.z.object({
    concept: zod_1.z.string().min(3),
    category: zod_1.z.nativeEnum(client_1.ExpenseCategory),
    amountCents: zod_1.z.number().int().nonnegative(),
    paymentMethod: zod_1.z.nativeEnum(client_1.PaymentMethod).nullable().optional(),
    notes: zod_1.z.string().optional(),
    incurredAt: zod_1.z.string().optional(),
    inventoryMovement: inventoryMovementInput.optional(),
    metadata: zod_1.z.record(zod_1.z.string(), zod_1.z.any()).optional(),
});
exports.UpdateExpenseDto = zod_1.z.object({
    concept: zod_1.z.string().min(3).optional(),
    category: zod_1.z.nativeEnum(client_1.ExpenseCategory).optional(),
    amountCents: zod_1.z.number().int().nonnegative().optional(),
    paymentMethod: zod_1.z.nativeEnum(client_1.PaymentMethod).nullable().optional(),
    notes: zod_1.z.string().optional(),
    incurredAt: zod_1.z.string().optional(),
});
exports.ExpenseQueryDto = zod_1.z.object({
    from: zod_1.z.string().optional(),
    to: zod_1.z.string().optional(),
    category: zod_1.z.nativeEnum(client_1.ExpenseCategory).optional(),
    recordedByUserId: zod_1.z.coerce.number().int().positive().optional(),
    search: zod_1.z.string().optional(),
    tzOffsetMinutes: zod_1.z.coerce.number().optional(),
});
exports.ExpensesSummaryQueryDto = exports.ExpenseQueryDto;
//# sourceMappingURL=expenses.dto.js.map