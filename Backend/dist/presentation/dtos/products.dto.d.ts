import { z } from 'zod';
export declare const CreateProductSimpleDto: z.ZodObject<{
    name: z.ZodString;
    categoryId: z.ZodNumber;
    priceCents: z.ZodNumber;
    description: z.ZodOptional<z.ZodString>;
    sku: z.ZodOptional<z.ZodString>;
}, z.core.$strip>;
export declare const VariantInputDto: z.ZodObject<{
    id: z.ZodOptional<z.ZodNumber>;
    name: z.ZodString;
    priceCents: z.ZodNumber;
    sku: z.ZodOptional<z.ZodString>;
}, z.core.$strip>;
export declare const CreateProductVariantedDto: z.ZodObject<{
    name: z.ZodString;
    categoryId: z.ZodNumber;
    variants: z.ZodArray<z.ZodObject<{
        name: z.ZodString;
        priceCents: z.ZodNumber;
        sku: z.ZodOptional<z.ZodString>;
    }, z.core.$strip>>;
    description: z.ZodOptional<z.ZodString>;
    sku: z.ZodOptional<z.ZodString>;
}, z.core.$strip>;
export declare const UpdateProductDto: z.ZodObject<{
    name: z.ZodOptional<z.ZodString>;
    categoryId: z.ZodOptional<z.ZodNumber>;
    priceCents: z.ZodOptional<z.ZodNullable<z.ZodNumber>>;
    description: z.ZodOptional<z.ZodString>;
    sku: z.ZodOptional<z.ZodString>;
    isActive: z.ZodOptional<z.ZodBoolean>;
}, z.core.$strip>;
export declare const ConvertToVariantedDto: z.ZodObject<{
    variants: z.ZodArray<z.ZodObject<{
        name: z.ZodString;
        priceCents: z.ZodNumber;
        sku: z.ZodOptional<z.ZodString>;
    }, z.core.$strip>>;
}, z.core.$strip>;
export declare const ConvertToSimpleDto: z.ZodObject<{
    priceCents: z.ZodNumber;
}, z.core.$strip>;
export type ConvertToVariantedDto = z.infer<typeof ConvertToVariantedDto>;
export type ConvertToSimpleDto = z.infer<typeof ConvertToSimpleDto>;
export declare const ReplaceVariantsDto: z.ZodObject<{
    variants: z.ZodArray<z.ZodObject<{
        id: z.ZodOptional<z.ZodNumber>;
        name: z.ZodString;
        priceCents: z.ZodNumber;
        sku: z.ZodOptional<z.ZodString>;
    }, z.core.$strip>>;
}, z.core.$strip>;
export declare const ListProductsQueryDto: z.ZodObject<{
    categorySlug: z.ZodOptional<z.ZodString>;
    type: z.ZodOptional<z.ZodEnum<{
        SIMPLE: "SIMPLE";
        VARIANTED: "VARIANTED";
        COMBO: "COMBO";
    }>>;
    isActive: z.ZodOptional<z.ZodCoercedBoolean<unknown>>;
}, z.core.$strip>;
export declare const UpdateAvailabilityDto: z.ZodObject<{
    isAvailable: z.ZodBoolean;
}, z.core.$strip>;
export type UpdateAvailabilityDtoType = z.infer<typeof UpdateAvailabilityDto>;
export declare const DetachModifierGroupDto: z.ZodUnion<readonly [z.ZodObject<{
    linkId: z.ZodNumber;
}, z.core.$strip>, z.ZodObject<{
    productId: z.ZodNumber;
    groupId: z.ZodNumber;
}, z.core.$strip>]>;
export declare const UpdateModifierGroupPositionDto: z.ZodObject<{
    linkId: z.ZodNumber;
    position: z.ZodNumber;
}, z.core.$strip>;
export declare const ReorderModifierGroupsDto: z.ZodObject<{
    productId: z.ZodNumber;
    items: z.ZodArray<z.ZodObject<{
        linkId: z.ZodNumber;
        position: z.ZodNumber;
    }, z.core.$strip>>;
}, z.core.$strip>;
export declare const AttachModifierGroupToVariantDto: z.ZodObject<{
    groupId: z.ZodNumber;
    minSelect: z.ZodOptional<z.ZodNumber>;
    maxSelect: z.ZodOptional<z.ZodNullable<z.ZodNumber>>;
    isRequired: z.ZodOptional<z.ZodBoolean>;
}, z.core.$strip>;
export declare const UpdateVariantModifierGroupDto: z.ZodObject<{
    minSelect: z.ZodOptional<z.ZodNumber>;
    maxSelect: z.ZodOptional<z.ZodNullable<z.ZodNumber>>;
    isRequired: z.ZodOptional<z.ZodBoolean>;
}, z.core.$strip>;
//# sourceMappingURL=products.dto.d.ts.map