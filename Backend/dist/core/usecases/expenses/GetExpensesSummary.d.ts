import type { ExpenseFilters, IExpenseRepository } from "../../domain/repositories/IExpenseRepository";
export declare class GetExpensesSummary {
    private repo;
    constructor(repo: IExpenseRepository);
    exec(filters?: ExpenseFilters): Promise<import("../../domain/repositories/IExpenseRepository").ExpenseSummaryResult>;
}
//# sourceMappingURL=GetExpensesSummary.d.ts.map