"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateMenuItemDto = exports.AddMenuItemDto = exports.UpdateMenuSectionDto = exports.CreateMenuSectionDto = exports.UpdateMenuDto = exports.CreateMenuDto = void 0;
const zod_1 = require("zod");
const client_1 = require("@prisma/client");
const dateOptional = zod_1.z
    .union([zod_1.z.string().datetime().nullable(), zod_1.z.coerce.date().nullable()])
    .optional()
    .transform((value) => {
    if (value === undefined)
        return undefined;
    if (value === null)
        return null;
    return value instanceof Date ? value : new Date(value);
});
exports.CreateMenuDto = zod_1.z.object({
    name: zod_1.z.string().min(2),
    isActive: zod_1.z.boolean().optional(),
    publishedAt: dateOptional,
    version: zod_1.z.number().int().positive().optional()
});
exports.UpdateMenuDto = zod_1.z.object({
    name: zod_1.z.string().min(2).optional(),
    isActive: zod_1.z.boolean().optional(),
    publishedAt: dateOptional,
    version: zod_1.z.number().int().positive().optional()
});
exports.CreateMenuSectionDto = zod_1.z.object({
    menuId: zod_1.z.number().int().positive(),
    name: zod_1.z.string().min(2),
    position: zod_1.z.number().int().nonnegative().optional(),
    categoryId: zod_1.z.number().int().positive().nullable().optional(),
    isActive: zod_1.z.boolean().optional()
});
exports.UpdateMenuSectionDto = zod_1.z.object({
    name: zod_1.z.string().min(2).optional(),
    position: zod_1.z.number().int().nonnegative().optional(),
    categoryId: zod_1.z.number().int().positive().nullable().optional(),
    isActive: zod_1.z.boolean().optional()
});
exports.AddMenuItemDto = zod_1.z.object({
    sectionId: zod_1.z.number().int().positive(),
    refType: zod_1.z.nativeEnum(client_1.MenuItemRefType),
    refId: zod_1.z.number().int().positive(),
    displayName: zod_1.z.string().min(1).nullable().optional(),
    displayPriceCents: zod_1.z.number().int().nonnegative().nullable().optional(),
    position: zod_1.z.number().int().nonnegative().optional(),
    isFeatured: zod_1.z.boolean().optional(),
    isActive: zod_1.z.boolean().optional()
});
exports.UpdateMenuItemDto = zod_1.z.object({
    displayName: zod_1.z.string().min(1).nullable().optional(),
    displayPriceCents: zod_1.z.number().int().nonnegative().nullable().optional(),
    position: zod_1.z.number().int().nonnegative().optional(),
    isFeatured: zod_1.z.boolean().optional(),
    isActive: zod_1.z.boolean().optional()
});
//# sourceMappingURL=menu.dto.js.map