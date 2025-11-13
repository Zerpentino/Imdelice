import { z } from 'zod';
export declare const CreateMenuDto: z.ZodObject<{
    name: z.ZodString;
    isActive: z.ZodOptional<z.ZodBoolean>;
    publishedAt: z.ZodPipe<z.ZodOptional<z.ZodUnion<readonly [z.ZodNullable<z.ZodString>, z.ZodNullable<z.ZodCoercedDate<unknown>>]>>, z.ZodTransform<Date | null | undefined, string | Date | null | undefined>>;
    version: z.ZodOptional<z.ZodNumber>;
}, z.core.$strip>;
export declare const UpdateMenuDto: z.ZodObject<{
    name: z.ZodOptional<z.ZodString>;
    isActive: z.ZodOptional<z.ZodBoolean>;
    publishedAt: z.ZodPipe<z.ZodOptional<z.ZodUnion<readonly [z.ZodNullable<z.ZodString>, z.ZodNullable<z.ZodCoercedDate<unknown>>]>>, z.ZodTransform<Date | null | undefined, string | Date | null | undefined>>;
    version: z.ZodOptional<z.ZodNumber>;
}, z.core.$strip>;
export declare const CreateMenuSectionDto: z.ZodObject<{
    menuId: z.ZodNumber;
    name: z.ZodString;
    position: z.ZodOptional<z.ZodNumber>;
    categoryId: z.ZodOptional<z.ZodNullable<z.ZodNumber>>;
    isActive: z.ZodOptional<z.ZodBoolean>;
}, z.core.$strip>;
export declare const UpdateMenuSectionDto: z.ZodObject<{
    name: z.ZodOptional<z.ZodString>;
    position: z.ZodOptional<z.ZodNumber>;
    categoryId: z.ZodOptional<z.ZodNullable<z.ZodNumber>>;
    isActive: z.ZodOptional<z.ZodBoolean>;
}, z.core.$strip>;
export declare const AddMenuItemDto: z.ZodObject<{
    sectionId: z.ZodNumber;
    refType: z.ZodEnum<{
        PRODUCT: "PRODUCT";
        VARIANT: "VARIANT";
        COMBO: "COMBO";
        OPTION: "OPTION";
    }>;
    refId: z.ZodNumber;
    displayName: z.ZodOptional<z.ZodNullable<z.ZodString>>;
    displayPriceCents: z.ZodOptional<z.ZodNullable<z.ZodNumber>>;
    position: z.ZodOptional<z.ZodNumber>;
    isFeatured: z.ZodOptional<z.ZodBoolean>;
    isActive: z.ZodOptional<z.ZodBoolean>;
}, z.core.$strip>;
export declare const UpdateMenuItemDto: z.ZodObject<{
    displayName: z.ZodOptional<z.ZodNullable<z.ZodString>>;
    displayPriceCents: z.ZodOptional<z.ZodNullable<z.ZodNumber>>;
    position: z.ZodOptional<z.ZodNumber>;
    isFeatured: z.ZodOptional<z.ZodBoolean>;
    isActive: z.ZodOptional<z.ZodBoolean>;
}, z.core.$strip>;
//# sourceMappingURL=menu.dto.d.ts.map