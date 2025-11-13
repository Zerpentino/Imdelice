"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateVariantModifierGroupDto = exports.AttachModifierGroupToVariantDto = exports.ReorderModifierGroupsDto = exports.UpdateModifierGroupPositionDto = exports.DetachModifierGroupDto = exports.UpdateAvailabilityDto = exports.ListProductsQueryDto = exports.ReplaceVariantsDto = exports.ConvertToSimpleDto = exports.ConvertToVariantedDto = exports.UpdateProductDto = exports.CreateProductVariantedDto = exports.VariantInputDto = exports.CreateProductSimpleDto = void 0;
const zod_1 = require("zod");
exports.CreateProductSimpleDto = zod_1.z.object({
    name: zod_1.z.string().min(2),
    categoryId: zod_1.z.number().int().positive(),
    priceCents: zod_1.z.number().int().nonnegative(),
    description: zod_1.z.string().optional(),
    sku: zod_1.z.string().optional(),
    // imageUrl: z.string().url().optional(),
});
exports.VariantInputDto = zod_1.z.object({
    id: zod_1.z.number().int().positive().optional(), // para updates
    name: zod_1.z.string().min(1),
    priceCents: zod_1.z.number().int().nonnegative(),
    sku: zod_1.z.string().optional(),
});
exports.CreateProductVariantedDto = zod_1.z.object({
    name: zod_1.z.string().min(2),
    categoryId: zod_1.z.number().int().positive(),
    variants: zod_1.z.array(exports.VariantInputDto.omit({ id: true })).min(1),
    description: zod_1.z.string().optional(),
    sku: zod_1.z.string().optional(),
    //imageUrl: z.string().url().optional(),
});
exports.UpdateProductDto = zod_1.z.object({
    name: zod_1.z.string().min(2).optional(),
    categoryId: zod_1.z.number().int().positive().optional(),
    priceCents: zod_1.z.number().int().nonnegative().nullable().optional(), // SIMPLE o null si varianted
    description: zod_1.z.string().optional(),
    sku: zod_1.z.string().optional(),
    //imageUrl: z.string().url().optional(),
    isActive: zod_1.z.boolean().optional()
});
// NUEVOS DTOs para conversión
exports.ConvertToVariantedDto = zod_1.z.object({
    variants: zod_1.z.array(zod_1.z.object({
        name: zod_1.z.string().min(1),
        priceCents: zod_1.z.number().int().nonnegative(),
        sku: zod_1.z.string().optional(),
    })).min(1),
});
exports.ConvertToSimpleDto = zod_1.z.object({
    priceCents: zod_1.z.number().int().nonnegative(),
});
exports.ReplaceVariantsDto = zod_1.z.object({
    variants: zod_1.z.array(exports.VariantInputDto).min(1)
});
exports.ListProductsQueryDto = zod_1.z.object({
    categorySlug: zod_1.z.string().optional(),
    type: zod_1.z.enum(['SIMPLE', 'VARIANTED', 'COMBO']).optional(),
    isActive: zod_1.z.coerce.boolean().optional(),
});
exports.UpdateAvailabilityDto = zod_1.z.object({
    isAvailable: zod_1.z.boolean()
});
exports.DetachModifierGroupDto = zod_1.z.union([
    zod_1.z.object({ linkId: zod_1.z.number().int().positive() }),
    zod_1.z.object({ productId: zod_1.z.number().int().positive(), groupId: zod_1.z.number().int().positive() })
]);
exports.UpdateModifierGroupPositionDto = zod_1.z.object({
    linkId: zod_1.z.number().int().positive(),
    position: zod_1.z.number().int().nonnegative()
});
exports.ReorderModifierGroupsDto = zod_1.z.object({
    productId: zod_1.z.number().int().positive(),
    items: zod_1.z.array(zod_1.z.object({
        linkId: zod_1.z.number().int().positive(),
        position: zod_1.z.number().int().nonnegative()
    })).min(1)
});
exports.AttachModifierGroupToVariantDto = zod_1.z.object({
    groupId: zod_1.z.number().int().positive(),
    minSelect: zod_1.z.number().int().nonnegative().optional(),
    maxSelect: zod_1.z.number().int().positive().nullable().optional(),
    isRequired: zod_1.z.boolean().optional()
});
exports.UpdateVariantModifierGroupDto = zod_1.z.object({
    minSelect: zod_1.z.number().int().nonnegative().optional(),
    maxSelect: zod_1.z.number().int().positive().nullable().optional(),
    isRequired: zod_1.z.boolean().optional()
});
//# sourceMappingURL=products.dto.js.map