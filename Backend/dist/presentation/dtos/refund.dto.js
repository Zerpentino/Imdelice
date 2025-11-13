"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.RefundOrderDto = void 0;
const zod_1 = require("zod");
exports.RefundOrderDto = zod_1.z
    .object({
    reason: zod_1.z.string().min(1).max(500).optional(),
    adminEmail: zod_1.z.string().email().optional(),
    adminPin: zod_1.z.string().min(4).max(10).optional(),
    password: zod_1.z.string().min(4),
})
    .refine((data) => data.adminEmail || data.adminPin, {
    message: "Debes enviar adminEmail o adminPin",
    path: ["adminEmail"],
});
//# sourceMappingURL=refund.dto.js.map