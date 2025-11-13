"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateModifierOptionDto = exports.ListProductsByGroupQueryDto = exports.AttachModifierToProductDto = exports.UpdateModifierGroupDto = exports.CreateModifierGroupDto = exports.ListModifierGroupsQueryDto = void 0;
const zod_1 = require("zod");
exports.ListModifierGroupsQueryDto = zod_1.z.object({
    isActive: zod_1.z.coerce.boolean().optional(),
    categoryId: zod_1.z.coerce.number().int().positive().optional(),
    categorySlug: zod_1.z.string().min(1).optional(),
    search: zod_1.z.string().min(1).optional(), // busca por nombre (ILIKE)
});
exports.CreateModifierGroupDto = zod_1.z.object({
    name: zod_1.z.string().min(2),
    description: zod_1.z.string().optional(),
    minSelect: zod_1.z.number().int().nonnegative().optional(),
    maxSelect: zod_1.z.number().int().positive().nullable().optional(),
    isRequired: zod_1.z.boolean().optional(),
    appliesToCategoryId: zod_1.z.coerce.number().int().positive().nullable().optional(),
    options: zod_1.z.array(zod_1.z.object({
        name: zod_1.z.string().min(1),
        priceExtraCents: zod_1.z.number().int().nonnegative().optional(),
        isDefault: zod_1.z.boolean().optional(),
        position: zod_1.z.number().int().nonnegative().optional(),
    })).min(1)
});
exports.UpdateModifierGroupDto = zod_1.z.object({
    name: zod_1.z.string().min(2).optional(),
    description: zod_1.z.string().optional(),
    minSelect: zod_1.z.number().int().nonnegative().optional(),
    maxSelect: zod_1.z.number().int().positive().nullable().optional(),
    isRequired: zod_1.z.boolean().optional(),
    isActive: zod_1.z.boolean().optional(),
    appliesToCategoryId: zod_1.z.coerce.number().int().positive().nullable().optional(),
    // estrategia simple: reemplazar todas las opciones
    replaceOptions: zod_1.z.array(zod_1.z.object({
        name: zod_1.z.string().min(1),
        priceExtraCents: zod_1.z.number().int().nonnegative().optional(),
        isDefault: zod_1.z.boolean().optional(),
        position: zod_1.z.number().int().nonnegative().optional(),
    })).optional()
});
exports.AttachModifierToProductDto = zod_1.z.object({
    productId: zod_1.z.number().int().positive(),
    groupId: zod_1.z.number().int().positive(),
    position: zod_1.z.number().int().nonnegative().optional(),
});
exports.ListProductsByGroupQueryDto = zod_1.z.object({
    isActive: zod_1.z.coerce.boolean().optional(),
    search: zod_1.z.string().min(1).optional(),
    limit: zod_1.z.coerce.number().int().positive().max(200).optional(),
    offset: zod_1.z.coerce.number().int().nonnegative().optional(),
});
// src/presentation/dtos/modifiers.dto.ts
exports.UpdateModifierOptionDto = zod_1.z.object({
    name: zod_1.z.string().min(1).optional(),
    priceExtraCents: zod_1.z.coerce.number().int().nonnegative().optional(),
    isDefault: zod_1.z.coerce.boolean().optional(),
    position: zod_1.z.coerce.number().int().nonnegative().optional(),
    isActive: zod_1.z.coerce.boolean().optional(),
});
//# sourceMappingURL=modifiers.dto.js.map