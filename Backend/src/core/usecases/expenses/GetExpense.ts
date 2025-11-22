import type { IExpenseRepository } from "../../domain/repositories/IExpenseRepository";

export class GetExpense {
  constructor(private repo: IExpenseRepository) {}

  exec(id: number) {
    return this.repo.getById(id);
  }
}
