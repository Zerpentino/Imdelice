"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateComboItemDto = exports.AddComboItemsDto = exports.CreateComboDto = void 0;
const zod_1 = require("zod");
exports.CreateComboDto = zod_1.z.object({
    name: zod_1.z.string().min(2),
    categoryId: zod_1.z.number().int().positive(),
    priceCents: zod_1.z.number().int().nonnegative(),
    description: zod_1.z.string().optional(),
    sku: zod_1.z.string().optional(),
    items: zod_1.z.array(zod_1.z.object({
        componentProductId: zod_1.z.number().int().positive(),
        componentVariantId: zod_1.z.number().int().positive().optional(),
        quantity: zod_1.z.number().int().positive().optional(),
        isRequired: zod_1.z.boolean().optional(),
        notes: zod_1.z.string().optional()
    })).optional()
});
exports.AddComboItemsDto = zod_1.z.object({
    items: zod_1.z.array(zod_1.z.object({
        componentProductId: zod_1.z.number().int().positive(),
        componentVariantId: zod_1.z.number().int().positive().optional(),
        quantity: zod_1.z.number().int().positive().optional(),
        isRequired: zod_1.z.boolean().optional(),
        notes: zod_1.z.string().optional()
    })).min(1)
});
exports.UpdateComboItemDto = zod_1.z.object({
    quantity: zod_1.z.number().int().positive().optional(),
    isRequired: zod_1.z.boolean().optional(),
    notes: zod_1.z.string().optional()
});
//# sourceMappingURL=products.combo.dto.js.map