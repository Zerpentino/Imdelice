import { z } from 'zod';
import { MenuItemRefType } from '@prisma/client';

const dateOptional = z
  .union([z.string().datetime().nullable(), z.coerce.date().nullable()])
  .optional()
  .transform((value) => {
    if (value === undefined) return undefined;
    if (value === null) return null;
    return value instanceof Date ? value : new Date(value);
  });

export const CreateMenuDto = z.object({
  name: z.string().min(2),
  isActive: z.boolean().optional(),
  publishedAt: dateOptional,
  version: z.number().int().positive().optional()
});
export const UpdateMenuDto = z.object({
  name: z.string().min(2).optional(),
  isActive: z.boolean().optional(),
  publishedAt: dateOptional,
  version: z.number().int().positive().optional()
});

export const CreateMenuSectionDto = z.object({
  menuId: z.number().int().positive(),
  name: z.string().min(2),
  position: z.number().int().nonnegative().optional(),
  categoryId: z.number().int().positive().nullable().optional(),
  isActive: z.boolean().optional()
});
export const UpdateMenuSectionDto = z.object({
  name: z.string().min(2).optional(),
  position: z.number().int().nonnegative().optional(),
  categoryId: z.number().int().positive().nullable().optional(),
  isActive: z.boolean().optional()
});

export const AddMenuItemDto = z.object({
  sectionId: z.number().int().positive(),
  refType: z.nativeEnum(MenuItemRefType),
  refId: z.number().int().positive(),
  displayName: z.string().min(1).nullable().optional(),
  displayPriceCents: z.number().int().nonnegative().nullable().optional(),
  position: z.number().int().nonnegative().optional(),
  isFeatured: z.boolean().optional(),
  isActive: z.boolean().optional()
});
export const UpdateMenuItemDto = z.object({
  displayName: z.string().min(1).nullable().optional(),
  displayPriceCents: z.number().int().nonnegative().nullable().optional(),
  position: z.number().int().nonnegative().optional(),
  isFeatured: z.boolean().optional(),
  isActive: z.boolean().optional()
});
