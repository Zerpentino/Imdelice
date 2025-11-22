import type { IOrderRepository, PaymentsReportFilters, PaymentsReportResult } from "../../domain/repositories/IOrderRepository";
import type { ExpenseFilters, ExpenseSummaryResult, IExpenseRepository } from "../../domain/repositories/IExpenseRepository";

export interface ProfitLossReportResult {
  payments: PaymentsReportResult;
  expenses: ExpenseSummaryResult;
  netAmountCents: number;
}

export class GetProfitLossReport {
  constructor(
    private orderRepo: IOrderRepository,
    private expenseRepo: IExpenseRepository,
  ) {}

  async exec(filters: PaymentsReportFilters): Promise<ProfitLossReportResult> {
    const payments = await this.orderRepo.getPaymentsReport(filters);
    const expenseFilters: ExpenseFilters = {
      from: filters.from,
      to: filters.to,
    };
    const expenses = await this.expenseRepo.getSummary(expenseFilters);
    const net = payments.grandTotals.amountCents - expenses.totalAmountCents;
    return { payments, expenses, netAmountCents: net };
  }
}
