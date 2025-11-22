import { z } from "zod";
export declare const InventoryMovementDto: z.ZodObject<{
    productId: z.ZodNumber;
    variantId: z.ZodOptional<z.ZodNullable<z.ZodNumber>>;
    locationId: z.ZodOptional<z.ZodNumber>;
    type: z.ZodEnum<{
        PURCHASE: "PURCHASE";
        SALE: "SALE";
        ADJUSTMENT: "ADJUSTMENT";
        WASTE: "WASTE";
        TRANSFER: "TRANSFER";
        SALE_RETURN: "SALE_RETURN";
    }>;
    quantity: z.ZodCoercedNumber<unknown>;
    reason: z.ZodOptional<z.ZodString>;
}, z.core.$strip>;
export declare const InventoryMovementByBarcodeDto: z.ZodObject<{
    barcode: z.ZodString;
    locationId: z.ZodOptional<z.ZodNumber>;
    type: z.ZodEnum<{
        PURCHASE: "PURCHASE";
        SALE: "SALE";
        ADJUSTMENT: "ADJUSTMENT";
        WASTE: "WASTE";
        TRANSFER: "TRANSFER";
        SALE_RETURN: "SALE_RETURN";
    }>;
    quantity: z.ZodCoercedNumber<unknown>;
    reason: z.ZodOptional<z.ZodString>;
}, z.core.$strip>;
export declare const ListInventoryItemsQueryDto: z.ZodObject<{
    id: z.ZodOptional<z.ZodCoercedNumber<unknown>>;
    productId: z.ZodOptional<z.ZodCoercedNumber<unknown>>;
    variantId: z.ZodOptional<z.ZodCoercedNumber<unknown>>;
    locationId: z.ZodOptional<z.ZodCoercedNumber<unknown>>;
    categoryId: z.ZodOptional<z.ZodCoercedNumber<unknown>>;
    search: z.ZodOptional<z.ZodString>;
}, z.core.$strip>;
export declare const InventoryMovementsQueryDto: z.ZodObject<{
    productId: z.ZodOptional<z.ZodCoercedNumber<unknown>>;
    variantId: z.ZodOptional<z.ZodCoercedNumber<unknown>>;
    locationId: z.ZodOptional<z.ZodCoercedNumber<unknown>>;
    orderId: z.ZodOptional<z.ZodCoercedNumber<unknown>>;
    type: z.ZodOptional<z.ZodEnum<{
        PURCHASE: "PURCHASE";
        SALE: "SALE";
        ADJUSTMENT: "ADJUSTMENT";
        WASTE: "WASTE";
        TRANSFER: "TRANSFER";
        SALE_RETURN: "SALE_RETURN";
    }>>;
    limit: z.ZodOptional<z.ZodCoercedNumber<unknown>>;
}, z.core.$strip>;
//# sourceMappingURL=inventory.dto.d.ts.map