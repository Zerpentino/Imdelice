"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.InventoryMovementsQueryDto = exports.ListInventoryItemsQueryDto = exports.InventoryMovementByBarcodeDto = exports.InventoryMovementDto = void 0;
const client_1 = require("@prisma/client");
const zod_1 = require("zod");
const nonZeroQuantity = zod_1.z
    .coerce
    .number()
    .refine((val) => val !== 0, { message: "quantity must be non-zero" });
exports.InventoryMovementDto = zod_1.z.object({
    productId: zod_1.z.number().int().positive(),
    variantId: zod_1.z.number().int().positive().nullable().optional(),
    locationId: zod_1.z.number().int().positive().optional(),
    type: zod_1.z.nativeEnum(client_1.InventoryMovementType),
    quantity: nonZeroQuantity,
    reason: zod_1.z.string().max(255).optional(),
});
exports.InventoryMovementByBarcodeDto = zod_1.z.object({
    barcode: zod_1.z.string().trim().min(3),
    locationId: zod_1.z.number().int().positive().optional(),
    type: zod_1.z.nativeEnum(client_1.InventoryMovementType),
    quantity: nonZeroQuantity,
    reason: zod_1.z.string().max(255).optional(),
});
exports.ListInventoryItemsQueryDto = zod_1.z.object({
    id: zod_1.z.coerce.number().int().positive().optional(),
    productId: zod_1.z.coerce.number().int().positive().optional(),
    variantId: zod_1.z.coerce.number().int().positive().optional(),
    locationId: zod_1.z.coerce.number().int().positive().optional(),
    categoryId: zod_1.z.coerce.number().int().positive().optional(),
    search: zod_1.z.string().optional(),
});
exports.InventoryMovementsQueryDto = zod_1.z.object({
    productId: zod_1.z.coerce.number().int().positive().optional(),
    variantId: zod_1.z.coerce.number().int().positive().optional(),
    locationId: zod_1.z.coerce.number().int().positive().optional(),
    orderId: zod_1.z.coerce.number().int().positive().optional(),
    type: zod_1.z.nativeEnum(client_1.InventoryMovementType).optional(),
    limit: zod_1.z.coerce.number().int().positive().max(200).optional(),
});
//# sourceMappingURL=inventory.dto.js.map