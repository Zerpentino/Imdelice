import { z } from "zod";
export declare const RefundOrderDto: z.ZodObject<{
    reason: z.ZodOptional<z.ZodString>;
    adminEmail: z.ZodOptional<z.ZodString>;
    adminPin: z.ZodOptional<z.ZodString>;
    password: z.ZodString;
}, z.core.$strip>;
export type RefundOrderDtoType = z.infer<typeof RefundOrderDto>;
//# sourceMappingURL=refund.dto.d.ts.map