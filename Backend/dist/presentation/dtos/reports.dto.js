"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.PaymentsReportQueryDto = void 0;
const zod_1 = require("zod");
exports.PaymentsReportQueryDto = zod_1.z.object({
    from: zod_1.z.string().optional(),
    to: zod_1.z.string().optional(),
    includeOrders: zod_1.z
        .union([zod_1.z.boolean(), zod_1.z.string()])
        .optional()
        .transform((value) => {
        if (typeof value === "boolean")
            return value;
        if (typeof value === "string") {
            const normalized = value.trim().toLowerCase();
            if (["1", "true", "yes"].includes(normalized))
                return true;
            if (["0", "false", "no", ""].includes(normalized))
                return false;
        }
        return undefined;
    }),
});
//# sourceMappingURL=reports.dto.js.map