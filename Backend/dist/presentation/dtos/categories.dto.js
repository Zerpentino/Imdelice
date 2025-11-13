"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateCategoryDto = exports.CreateCategoryDto = void 0;
const zod_1 = require("zod");
exports.CreateCategoryDto = zod_1.z.object({
    name: zod_1.z.string().min(2),
    slug: zod_1.z.string().min(2),
    parentId: zod_1.z.number().int().positive().nullable().optional(),
    position: zod_1.z.number().int().nonnegative().optional(),
    isComboOnly: zod_1.z.boolean().optional() // 👈 nuevo (opcional)
});
exports.UpdateCategoryDto = zod_1.z.object({
    name: zod_1.z.string().min(2).optional(),
    slug: zod_1.z.string().min(2).optional(),
    parentId: zod_1.z.number().int().positive().nullable().optional(),
    position: zod_1.z.number().int().nonnegative().optional(),
    isActive: zod_1.z.boolean().optional(),
    isComboOnly: zod_1.z.boolean().optional() // 👈 nuevo
});
//# sourceMappingURL=categories.dto.js.map