import { Prisma } from "@prisma/client";
import type { CreateExpenseInput, ExpenseFilters, ExpenseSummaryResult, IExpenseRepository, UpdateExpenseInput } from "../../core/domain/repositories/IExpenseRepository";
export declare class PrismaExpenseRepository implements IExpenseRepository {
    create(data: CreateExpenseInput): Prisma.Prisma__ExpenseClient<{
        id: number;
        category: import(".prisma/client").$Enums.ExpenseCategory;
        notes: string | null;
        metadata: Prisma.JsonValue | null;
        concept: string;
        amountCents: number;
        paymentMethod: import(".prisma/client").$Enums.PaymentMethod | null;
        incurredAt: Date;
        recordedAt: Date;
        recordedByUserId: number;
        inventoryMovementId: number | null;
    }, never, import("@prisma/client/runtime/library").DefaultArgs, Prisma.PrismaClientOptions>;
    list(filters?: ExpenseFilters): Prisma.PrismaPromise<({
        inventoryMovement: ({
            product: {
                name: string;
                id: number;
                sku: string | null;
                barcode: string | null;
            };
            variant: {
                name: string;
                id: number;
            } | null;
            location: {
                name: string;
                id: number;
            } | null;
        } & {
            id: number;
            createdAt: Date;
            type: import(".prisma/client").$Enums.InventoryMovementType;
            productId: number;
            variantId: number | null;
            quantity: Prisma.Decimal;
            unit: import(".prisma/client").$Enums.UnitOfMeasure;
            locationId: number | null;
            reason: string | null;
            metadata: Prisma.JsonValue | null;
            relatedOrderId: number | null;
            performedByUserId: number | null;
        }) | null;
        recordedBy: {
            name: string | null;
            id: number;
            email: string;
        };
    } & {
        id: number;
        category: import(".prisma/client").$Enums.ExpenseCategory;
        notes: string | null;
        metadata: Prisma.JsonValue | null;
        concept: string;
        amountCents: number;
        paymentMethod: import(".prisma/client").$Enums.PaymentMethod | null;
        incurredAt: Date;
        recordedAt: Date;
        recordedByUserId: number;
        inventoryMovementId: number | null;
    })[]>;
    getById(id: number): Prisma.Prisma__ExpenseClient<({
        inventoryMovement: ({
            product: {
                name: string;
                id: number;
                sku: string | null;
                barcode: string | null;
            };
            variant: {
                name: string;
                id: number;
            } | null;
            location: {
                name: string;
                id: number;
            } | null;
        } & {
            id: number;
            createdAt: Date;
            type: import(".prisma/client").$Enums.InventoryMovementType;
            productId: number;
            variantId: number | null;
            quantity: Prisma.Decimal;
            unit: import(".prisma/client").$Enums.UnitOfMeasure;
            locationId: number | null;
            reason: string | null;
            metadata: Prisma.JsonValue | null;
            relatedOrderId: number | null;
            performedByUserId: number | null;
        }) | null;
        recordedBy: {
            name: string | null;
            id: number;
            email: string;
        };
    } & {
        id: number;
        category: import(".prisma/client").$Enums.ExpenseCategory;
        notes: string | null;
        metadata: Prisma.JsonValue | null;
        concept: string;
        amountCents: number;
        paymentMethod: import(".prisma/client").$Enums.PaymentMethod | null;
        incurredAt: Date;
        recordedAt: Date;
        recordedByUserId: number;
        inventoryMovementId: number | null;
    }) | null, null, import("@prisma/client/runtime/library").DefaultArgs, Prisma.PrismaClientOptions>;
    update(id: number, data: UpdateExpenseInput): Prisma.Prisma__ExpenseClient<{
        id: number;
        category: import(".prisma/client").$Enums.ExpenseCategory;
        notes: string | null;
        metadata: Prisma.JsonValue | null;
        concept: string;
        amountCents: number;
        paymentMethod: import(".prisma/client").$Enums.PaymentMethod | null;
        incurredAt: Date;
        recordedAt: Date;
        recordedByUserId: number;
        inventoryMovementId: number | null;
    }, never, import("@prisma/client/runtime/library").DefaultArgs, Prisma.PrismaClientOptions>;
    delete(id: number): Promise<void>;
    getSummary(filters?: ExpenseFilters): Promise<ExpenseSummaryResult>;
}
//# sourceMappingURL=PrismaExpenseRepository.d.ts.map