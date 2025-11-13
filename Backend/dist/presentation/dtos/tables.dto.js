"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateTableDto = exports.CreateTableDto = void 0;
const zod_1 = require("zod");
const seatsSchema = zod_1.z
    .number()
    .int()
    .positive()
    .nullable()
    .optional();
exports.CreateTableDto = zod_1.z.object({
    name: zod_1.z.string().min(1),
    seats: seatsSchema,
    isActive: zod_1.z.boolean().optional()
});
exports.UpdateTableDto = zod_1.z
    .object({
    name: zod_1.z.string().min(1).optional(),
    seats: seatsSchema,
    isActive: zod_1.z.boolean().optional()
})
    .refine((data) => Object.keys(data).length > 0, {
    message: 'Debe proporcionar al menos un campo para actualizar.'
});
//# sourceMappingURL=tables.dto.js.map