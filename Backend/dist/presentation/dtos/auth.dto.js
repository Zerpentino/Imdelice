"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.LoginByPinDto = exports.LoginByEmailDto = void 0;
const zod_1 = require("zod");
exports.LoginByEmailDto = zod_1.z.object({
    email: zod_1.z.string().email(),
    password: zod_1.z.string().min(3)
});
exports.LoginByPinDto = zod_1.z.object({
    pinCode: zod_1.z.string().length(4).regex(/^\d{4}$/),
    password: zod_1.z.string().min(3)
});
//# sourceMappingURL=auth.dto.js.map