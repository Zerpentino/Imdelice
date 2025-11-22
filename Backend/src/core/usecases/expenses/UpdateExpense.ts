import type { IExpenseRepository, UpdateExpenseInput } from "../../domain/repositories/IExpenseRepository";

export class UpdateExpense {
  constructor(private repo: IExpenseRepository) {}

  exec(id: number, input: UpdateExpenseInput) {
    return this.repo.update(id, input);
  }
}
