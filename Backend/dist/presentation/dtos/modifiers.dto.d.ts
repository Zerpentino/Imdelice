import { z } from 'zod';
export declare const ListModifierGroupsQueryDto: z.ZodObject<{
    isActive: z.ZodOptional<z.ZodCoercedBoolean<unknown>>;
    categoryId: z.ZodOptional<z.ZodCoercedNumber<unknown>>;
    categorySlug: z.ZodOptional<z.ZodString>;
    search: z.ZodOptional<z.ZodString>;
}, z.core.$strip>;
export declare const CreateModifierGroupDto: z.ZodObject<{
    name: z.ZodString;
    description: z.ZodOptional<z.ZodString>;
    minSelect: z.ZodOptional<z.ZodNumber>;
    maxSelect: z.ZodOptional<z.ZodNullable<z.ZodNumber>>;
    isRequired: z.ZodOptional<z.ZodBoolean>;
    appliesToCategoryId: z.ZodOptional<z.ZodNullable<z.ZodCoercedNumber<unknown>>>;
    options: z.ZodArray<z.ZodObject<{
        name: z.ZodString;
        priceExtraCents: z.ZodOptional<z.ZodNumber>;
        isDefault: z.ZodOptional<z.ZodBoolean>;
        position: z.ZodOptional<z.ZodNumber>;
    }, z.core.$strip>>;
}, z.core.$strip>;
export declare const UpdateModifierGroupDto: z.ZodObject<{
    name: z.ZodOptional<z.ZodString>;
    description: z.ZodOptional<z.ZodString>;
    minSelect: z.ZodOptional<z.ZodNumber>;
    maxSelect: z.ZodOptional<z.ZodNullable<z.ZodNumber>>;
    isRequired: z.ZodOptional<z.ZodBoolean>;
    isActive: z.ZodOptional<z.ZodBoolean>;
    appliesToCategoryId: z.ZodOptional<z.ZodNullable<z.ZodCoercedNumber<unknown>>>;
    replaceOptions: z.ZodOptional<z.ZodArray<z.ZodObject<{
        name: z.ZodString;
        priceExtraCents: z.ZodOptional<z.ZodNumber>;
        isDefault: z.ZodOptional<z.ZodBoolean>;
        position: z.ZodOptional<z.ZodNumber>;
    }, z.core.$strip>>>;
}, z.core.$strip>;
export declare const AttachModifierToProductDto: z.ZodObject<{
    productId: z.ZodNumber;
    groupId: z.ZodNumber;
    position: z.ZodOptional<z.ZodNumber>;
}, z.core.$strip>;
export declare const ListProductsByGroupQueryDto: z.ZodObject<{
    isActive: z.ZodOptional<z.ZodCoercedBoolean<unknown>>;
    search: z.ZodOptional<z.ZodString>;
    limit: z.ZodOptional<z.ZodCoercedNumber<unknown>>;
    offset: z.ZodOptional<z.ZodCoercedNumber<unknown>>;
}, z.core.$strip>;
export declare const UpdateModifierOptionDto: z.ZodObject<{
    name: z.ZodOptional<z.ZodString>;
    priceExtraCents: z.ZodOptional<z.ZodCoercedNumber<unknown>>;
    isDefault: z.ZodOptional<z.ZodCoercedBoolean<unknown>>;
    position: z.ZodOptional<z.ZodCoercedNumber<unknown>>;
    isActive: z.ZodOptional<z.ZodCoercedBoolean<unknown>>;
}, z.core.$strip>;
//# sourceMappingURL=modifiers.dto.d.ts.map