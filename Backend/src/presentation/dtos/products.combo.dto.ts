import { z } from 'zod';

export const CreateComboDto = z.object({
  name: z.string().min(2),
  categoryId: z.number().int().positive(),
  priceCents: z.number().int().nonnegative(),
  description: z.string().optional(),
  sku: z.string().optional(),
  items: z.array(z.object({
    componentProductId: z.number().int().positive(),
    componentVariantId: z.number().int().positive().optional(),
    quantity: z.number().int().positive().optional(),
    isRequired: z.boolean().optional(),
    notes: z.string().optional()
  })).optional()
});
export type CreateComboDtoType = z.infer<typeof CreateComboDto>;

export const AddComboItemsDto = z.object({
  items: z.array(z.object({
    componentProductId: z.number().int().positive(),
    componentVariantId: z.number().int().positive().optional(),
    quantity: z.number().int().positive().optional(),
    isRequired: z.boolean().optional(),
    notes: z.string().optional()
  })).min(1)
});

export const UpdateComboItemDto = z.object({
  quantity: z.number().int().positive().optional(),
  isRequired: z.boolean().optional(),
  notes: z.string().optional()
});
