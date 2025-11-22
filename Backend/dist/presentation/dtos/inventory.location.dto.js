"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateInventoryLocationDto = exports.CreateInventoryLocationDto = void 0;
const client_1 = require("@prisma/client");
const zod_1 = require("zod");
exports.CreateInventoryLocationDto = zod_1.z.object({
    name: zod_1.z.string().min(2),
    code: zod_1.z
        .string()
        .trim()
        .min(1)
        .max(20)
        .optional(),
    type: zod_1.z.nativeEnum(client_1.InventoryLocationType).optional(),
    isDefault: zod_1.z.boolean().optional(),
});
exports.UpdateInventoryLocationDto = zod_1.z.object({
    name: zod_1.z.string().min(2).optional(),
    code: zod_1.z
        .string()
        .trim()
        .min(1)
        .max(20)
        .nullable()
        .optional(),
    type: zod_1.z.nativeEnum(client_1.InventoryLocationType).optional(),
    isDefault: zod_1.z.boolean().optional(),
    isActive: zod_1.z.boolean().optional(),
});
//# sourceMappingURL=inventory.location.dto.js.map