import { InventoryLocationType } from "@prisma/client";
import { z } from "zod";

export const CreateInventoryLocationDto = z.object({
  name: z.string().min(2),
  code: z
    .string()
    .trim()
    .min(1)
    .max(20)
    .optional(),
  type: z.nativeEnum(InventoryLocationType).optional(),
  isDefault: z.boolean().optional(),
});

export const UpdateInventoryLocationDto = z.object({
  name: z.string().min(2).optional(),
  code: z
    .string()
    .trim()
    .min(1)
    .max(20)
    .nullable()
    .optional(),
  type: z.nativeEnum(InventoryLocationType).optional(),
  isDefault: z.boolean().optional(),
  isActive: z.boolean().optional(),
});
