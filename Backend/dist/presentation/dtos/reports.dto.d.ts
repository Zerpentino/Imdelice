import { z } from "zod";
export declare const PaymentsReportQueryDto: z.ZodObject<{
    from: z.ZodOptional<z.ZodString>;
    to: z.ZodOptional<z.ZodString>;
    includeOrders: z.ZodPipe<z.ZodOptional<z.ZodUnion<readonly [z.ZodBoolean, z.ZodString]>>, z.ZodTransform<boolean | undefined, string | boolean | undefined>>;
}, z.core.$strip>;
export type PaymentsReportQueryDtoType = z.infer<typeof PaymentsReportQueryDto>;
//# sourceMappingURL=reports.dto.d.ts.map