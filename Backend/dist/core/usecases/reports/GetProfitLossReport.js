"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.GetProfitLossReport = void 0;
class GetProfitLossReport {
    constructor(orderRepo, expenseRepo) {
        this.orderRepo = orderRepo;
        this.expenseRepo = expenseRepo;
    }
    async exec(filters) {
        const payments = await this.orderRepo.getPaymentsReport(filters);
        const expenseFilters = {
            from: filters.from,
            to: filters.to,
        };
        const expenses = await this.expenseRepo.getSummary(expenseFilters);
        const net = payments.grandTotals.amountCents - expenses.totalAmountCents;
        return { payments, expenses, netAmountCents: net };
    }
}
exports.GetProfitLossReport = GetProfitLossReport;
//# sourceMappingURL=GetProfitLossReport.js.map