import type { ExpenseFilters, IExpenseRepository } from "../../domain/repositories/IExpenseRepository";
export declare class ListExpenses {
    private repo;
    constructor(repo: IExpenseRepository);
    exec(filters?: ExpenseFilters): Promise<{
        id: number;
        category: import(".prisma/client").$Enums.ExpenseCategory;
        notes: string | null;
        metadata: import("@prisma/client/runtime/library").JsonValue | null;
        concept: string;
        amountCents: number;
        paymentMethod: import(".prisma/client").$Enums.PaymentMethod | null;
        incurredAt: Date;
        recordedAt: Date;
        recordedByUserId: number;
        inventoryMovementId: number | null;
    }[]>;
}
//# sourceMappingURL=ListExpenses.d.ts.map