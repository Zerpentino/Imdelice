"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateRoleDto = exports.CreateRoleDto = void 0;
const zod_1 = require("zod");
exports.CreateRoleDto = zod_1.z.object({
    name: zod_1.z.string().min(2),
    description: zod_1.z.string().nullish(),
    permissionCodes: zod_1.z.array(zod_1.z.string()).default([]) // 👈
});
exports.UpdateRoleDto = zod_1.z.object({
    name: zod_1.z.string().min(2).optional(),
    description: zod_1.z.string().optional(),
    permissionCodes: zod_1.z.array(zod_1.z.string()).optional() // 👈
});
//# sourceMappingURL=roles.dto.js.map