import type { IExpenseRepository } from "../../domain/repositories/IExpenseRepository";

export class DeleteExpense {
  constructor(private repo: IExpenseRepository) {}

  exec(id: number) {
    return this.repo.delete(id);
  }
}
