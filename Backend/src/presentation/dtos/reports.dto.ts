import { z } from "zod";

export const PaymentsReportQueryDto = z.object({
  from: z.string().optional(),
  to: z.string().optional(),
  includeOrders: z
    .union([z.boolean(), z.string()])
    .optional()
    .transform((value) => {
      if (typeof value === "boolean") return value;
      if (typeof value === "string") {
        const normalized = value.trim().toLowerCase();
        if (["1", "true", "yes"].includes(normalized)) return true;
        if (["0", "false", "no", ""].includes(normalized)) return false;
      }
      return undefined;
    }),
});

export type PaymentsReportQueryDtoType = z.infer<typeof PaymentsReportQueryDto>;
