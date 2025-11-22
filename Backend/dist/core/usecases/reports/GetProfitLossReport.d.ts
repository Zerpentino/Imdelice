import type { IOrderRepository, PaymentsReportFilters, PaymentsReportResult } from "../../domain/repositories/IOrderRepository";
import type { ExpenseSummaryResult, IExpenseRepository } from "../../domain/repositories/IExpenseRepository";
export interface ProfitLossReportResult {
    payments: PaymentsReportResult;
    expenses: ExpenseSummaryResult;
    netAmountCents: number;
}
export declare class GetProfitLossReport {
    private orderRepo;
    private expenseRepo;
    constructor(orderRepo: IOrderRepository, expenseRepo: IExpenseRepository);
    exec(filters: PaymentsReportFilters): Promise<ProfitLossReportResult>;
}
//# sourceMappingURL=GetProfitLossReport.d.ts.map