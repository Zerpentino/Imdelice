import type { ExpenseFilters, IExpenseRepository } from "../../domain/repositories/IExpenseRepository";

export class ListExpenses {
  constructor(private repo: IExpenseRepository) {}

  exec(filters?: ExpenseFilters) {
    return this.repo.list(filters);
  }
}
