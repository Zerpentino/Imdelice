import type { CreateExpenseInput, IExpenseRepository } from "../../domain/repositories/IExpenseRepository";
export declare class CreateExpense {
    private repo;
    constructor(repo: IExpenseRepository);
    exec(input: CreateExpenseInput): Promise<{
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
    }>;
}
//# sourceMappingURL=CreateExpense.d.ts.map