import { z } from "zod";

export const RefundOrderDto = z
  .object({
    reason: z.string().min(1).max(500).optional(),
    adminEmail: z.string().email().optional(),
    adminPin: z.string().min(4).max(10).optional(),
    password: z.string().min(4),
  })
  .refine((data) => data.adminEmail || data.adminPin, {
    message: "Debes enviar adminEmail o adminPin",
    path: ["adminEmail"],
  });

export type RefundOrderDtoType = z.infer<typeof RefundOrderDto>;
