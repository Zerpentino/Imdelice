import { z } from 'zod';

export const ListModifierGroupsQueryDto = z.object({
  isActive: z.coerce.boolean().optional(),
  categoryId: z.coerce.number().int().positive().optional(),
  categorySlug: z.string().min(1).optional(),
  search: z.string().min(1).optional(), // busca por nombre (ILIKE)
});

export const CreateModifierGroupDto = z.object({
  name: z.string().min(2),
  description: z.string().optional(),
  minSelect: z.number().int().nonnegative().optional(),
  maxSelect: z.number().int().positive().nullable().optional(),
  isRequired: z.boolean().optional(),
    appliesToCategoryId: z.coerce.number().int().positive().nullable().optional(),

  options: z.array(z.object({
    name: z.string().min(1),
    priceExtraCents: z.number().int().nonnegative().optional(),
    isDefault: z.boolean().optional(),
    position: z.number().int().nonnegative().optional(),
  })).min(1)
});

export const UpdateModifierGroupDto = z.object({
  name: z.string().min(2).optional(),
  description: z.string().optional(),
  minSelect: z.number().int().nonnegative().optional(),
  maxSelect: z.number().int().positive().nullable().optional(),
  isRequired: z.boolean().optional(),
  isActive: z.boolean().optional(),
    appliesToCategoryId: z.coerce.number().int().positive().nullable().optional(),

  // estrategia simple: reemplazar todas las opciones
  replaceOptions: z.array(z.object({
    name: z.string().min(1),
    priceExtraCents: z.number().int().nonnegative().optional(),
    isDefault: z.boolean().optional(),
    position: z.number().int().nonnegative().optional(),
  })).optional()
});

export const AttachModifierToProductDto = z.object({
  productId: z.number().int().positive(),
  groupId: z.number().int().positive(),
  position: z.number().int().nonnegative().optional(),
});
export const ListProductsByGroupQueryDto = z.object({
  isActive: z.coerce.boolean().optional(),
  search:   z.string().min(1).optional(),
  limit:    z.coerce.number().int().positive().max(200).optional(),
  offset:   z.coerce.number().int().nonnegative().optional(),
});
// src/presentation/dtos/modifiers.dto.ts

export const UpdateModifierOptionDto = z.object({
  name: z.string().min(1).optional(),
  priceExtraCents: z.coerce.number().int().nonnegative().optional(),
  isDefault: z.coerce.boolean().optional(),
  position: z.coerce.number().int().nonnegative().optional(),
  isActive: z.coerce.boolean().optional(),
});
