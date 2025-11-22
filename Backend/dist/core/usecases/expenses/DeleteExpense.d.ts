import type { IExpenseRepository } from "../../domain/repositories/IExpenseRepository";
export declare class DeleteExpense {
    private repo;
    constructor(repo: IExpenseRepository);
    exec(id: number): Promise<void>;
}
//# sourceMappingURL=DeleteExpense.d.ts.map