import { z } from "zod";
declare const inventoryMovementInput: z.ZodObject<{
    productId: z.ZodOptional<z.ZodNumber>;
    variantId: z.ZodOptional<z.ZodNullable<z.ZodNumber>>;
    locationId: z.ZodOptional<z.ZodNumber>;
    barcode: z.ZodOptional<z.ZodString>;
    type: z.ZodDefault<z.ZodEnum<{
        PURCHASE: "PURCHASE";
        SALE: "SALE";
        ADJUSTMENT: "ADJUSTMENT";
        WASTE: "WASTE";
        TRANSFER: "TRANSFER";
        SALE_RETURN: "SALE_RETURN";
    }>>;
    quantity: z.ZodNumber;
    reason: z.ZodOptional<z.ZodString>;
}, z.core.$strip>;
export declare const CreateExpenseDto: z.ZodObject<{
    concept: z.ZodString;
    category: z.ZodEnum<{
        SUPPLIES: "SUPPLIES";
        MAINTENANCE: "MAINTENANCE";
        LOSS: "LOSS";
        OTHER: "OTHER";
    }>;
    amountCents: z.ZodNumber;
    paymentMethod: z.ZodOptional<z.ZodNullable<z.ZodEnum<{
        CASH: "CASH";
        CARD: "CARD";
        TRANSFER: "TRANSFER";
        OTHER: "OTHER";
    }>>>;
    notes: z.ZodOptional<z.ZodString>;
    incurredAt: z.ZodOptional<z.ZodString>;
    inventoryMovement: z.ZodOptional<z.ZodObject<{
        productId: z.ZodOptional<z.ZodNumber>;
        variantId: z.ZodOptional<z.ZodNullable<z.ZodNumber>>;
        locationId: z.ZodOptional<z.ZodNumber>;
        barcode: z.ZodOptional<z.ZodString>;
        type: z.ZodDefault<z.ZodEnum<{
            PURCHASE: "PURCHASE";
            SALE: "SALE";
            ADJUSTMENT: "ADJUSTMENT";
            WASTE: "WASTE";
            TRANSFER: "TRANSFER";
            SALE_RETURN: "SALE_RETURN";
        }>>;
        quantity: z.ZodNumber;
        reason: z.ZodOptional<z.ZodString>;
    }, z.core.$strip>>;
    metadata: z.ZodOptional<z.ZodRecord<z.ZodString, z.ZodAny>>;
}, z.core.$strip>;
export declare const UpdateExpenseDto: z.ZodObject<{
    concept: z.ZodOptional<z.ZodString>;
    category: z.ZodOptional<z.ZodEnum<{
        SUPPLIES: "SUPPLIES";
        MAINTENANCE: "MAINTENANCE";
        LOSS: "LOSS";
        OTHER: "OTHER";
    }>>;
    amountCents: z.ZodOptional<z.ZodNumber>;
    paymentMethod: z.ZodOptional<z.ZodNullable<z.ZodEnum<{
        CASH: "CASH";
        CARD: "CARD";
        TRANSFER: "TRANSFER";
        OTHER: "OTHER";
    }>>>;
    notes: z.ZodOptional<z.ZodString>;
    incurredAt: z.ZodOptional<z.ZodString>;
}, z.core.$strip>;
export declare const ExpenseQueryDto: z.ZodObject<{
    from: z.ZodOptional<z.ZodString>;
    to: z.ZodOptional<z.ZodString>;
    category: z.ZodOptional<z.ZodEnum<{
        SUPPLIES: "SUPPLIES";
        MAINTENANCE: "MAINTENANCE";
        LOSS: "LOSS";
        OTHER: "OTHER";
    }>>;
    recordedByUserId: z.ZodOptional<z.ZodCoercedNumber<unknown>>;
    search: z.ZodOptional<z.ZodString>;
    tzOffsetMinutes: z.ZodOptional<z.ZodCoercedNumber<unknown>>;
}, z.core.$strip>;
export declare const ExpensesSummaryQueryDto: z.ZodObject<{
    from: z.ZodOptional<z.ZodString>;
    to: z.ZodOptional<z.ZodString>;
    category: z.ZodOptional<z.ZodEnum<{
        SUPPLIES: "SUPPLIES";
        MAINTENANCE: "MAINTENANCE";
        LOSS: "LOSS";
        OTHER: "OTHER";
    }>>;
    recordedByUserId: z.ZodOptional<z.ZodCoercedNumber<unknown>>;
    search: z.ZodOptional<z.ZodString>;
    tzOffsetMinutes: z.ZodOptional<z.ZodCoercedNumber<unknown>>;
}, z.core.$strip>;
export type InventoryMovementInputDto = z.infer<typeof inventoryMovementInput>;
export {};
//# sourceMappingURL=expenses.dto.d.ts.map