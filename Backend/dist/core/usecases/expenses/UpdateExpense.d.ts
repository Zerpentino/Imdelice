import type { IExpenseRepository, UpdateExpenseInput } from "../../domain/repositories/IExpenseRepository";
export declare class UpdateExpense {
    private repo;
    constructor(repo: IExpenseRepository);
    exec(id: number, input: UpdateExpenseInput): Promise<{
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
//# sourceMappingURL=UpdateExpense.d.ts.map