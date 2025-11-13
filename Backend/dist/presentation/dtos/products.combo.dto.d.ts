import { z } from 'zod';
export declare const CreateComboDto: z.ZodObject<{
    name: z.ZodString;
    categoryId: z.ZodNumber;
    priceCents: z.ZodNumber;
    description: z.ZodOptional<z.ZodString>;
    sku: z.ZodOptional<z.ZodString>;
    items: z.ZodOptional<z.ZodArray<z.ZodObject<{
        componentProductId: z.ZodNumber;
        componentVariantId: z.ZodOptional<z.ZodNumber>;
        quantity: z.ZodOptional<z.ZodNumber>;
        isRequired: z.ZodOptional<z.ZodBoolean>;
        notes: z.ZodOptional<z.ZodString>;
    }, z.core.$strip>>>;
}, z.core.$strip>;
export type CreateComboDtoType = z.infer<typeof CreateComboDto>;
export declare const AddComboItemsDto: z.ZodObject<{
    items: z.ZodArray<z.ZodObject<{
        componentProductId: z.ZodNumber;
        componentVariantId: z.ZodOptional<z.ZodNumber>;
        quantity: z.ZodOptional<z.ZodNumber>;
        isRequired: z.ZodOptional<z.ZodBoolean>;
        notes: z.ZodOptional<z.ZodString>;
    }, z.core.$strip>>;
}, z.core.$strip>;
export declare const UpdateComboItemDto: z.ZodObject<{
    quantity: z.ZodOptional<z.ZodNumber>;
    isRequired: z.ZodOptional<z.ZodBoolean>;
    notes: z.ZodOptional<z.ZodString>;
}, z.core.$strip>;
//# sourceMappingURL=products.combo.dto.d.ts.map