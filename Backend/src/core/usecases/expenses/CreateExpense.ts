import type { CreateExpenseInput, IExpenseRepository } from "../../domain/repositories/IExpenseRepository";

export class CreateExpense {
  constructor(private repo: IExpenseRepository) {}

  exec(input: CreateExpenseInput) {
    return this.repo.create(input);
  }
}
