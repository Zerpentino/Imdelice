import { z } from 'zod';
export declare const CreateCategoryDto: z.ZodObject<{
    name: z.ZodString;
    slug: z.ZodString;
    parentId: z.ZodOptional<z.ZodNullable<z.ZodNumber>>;
    position: z.ZodOptional<z.ZodNumber>;
    isComboOnly: z.ZodOptional<z.ZodBoolean>;
}, z.core.$strip>;
export type CreateCategoryDtoType = z.infer<typeof CreateCategoryDto>;
export declare const UpdateCategoryDto: z.ZodObject<{
    name: z.ZodOptional<z.ZodString>;
    slug: z.ZodOptional<z.ZodString>;
    parentId: z.ZodOptional<z.ZodNullable<z.ZodNumber>>;
    position: z.ZodOptional<z.ZodNumber>;
    isActive: z.ZodOptional<z.ZodBoolean>;
    isComboOnly: z.ZodOptional<z.ZodBoolean>;
}, z.core.$strip>;
export type UpdateCategoryDtoType = z.infer<typeof UpdateCategoryDto>;
//# sourceMappingURL=categories.dto.d.ts.map