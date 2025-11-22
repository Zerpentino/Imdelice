import type { ExpenseFilters, IExpenseRepository } from "../../domain/repositories/IExpenseRepository";

export class GetExpensesSummary {
  constructor(private repo: IExpenseRepository) {}

  exec(filters?: ExpenseFilters) {
    return this.repo.getSummary(filters);
  }
}
