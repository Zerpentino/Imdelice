"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateUserDto = exports.CreateUserDto = void 0;
const zod_1 = require("zod");
exports.CreateUserDto = zod_1.z.object({
    email: zod_1.z.string().email(),
    name: zod_1.z.string().optional(),
    roleId: zod_1.z.number().int().positive(),
    password: zod_1.z.string().min(3),
    pinCode: zod_1.z.string().length(4).regex(/^\d{4}$/).optional() // '1234'
});
exports.UpdateUserDto = zod_1.z.object({
    email: zod_1.z.string().email().optional(),
    name: zod_1.z.string().optional(),
    roleId: zod_1.z.number().int().positive().optional(),
    password: zod_1.z.string().min(3).optional(),
    pinCode: zod_1.z.string().length(4).regex(/^\d{4}$/).nullable().optional()
});
//# sourceMappingURL=users.dto.js.map