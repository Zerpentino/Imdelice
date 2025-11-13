import type { IOrderRepository } from "../../domain/repositories/IOrderRepository";

export class UpdateOrderItem {
  constructor(private repo: IOrderRepository) {}
  exec(orderItemId: number, data: {
    quantity?: number; notes?: string | null;
    replaceModifiers?: { optionId: number; quantity?: number }[];
  }) {
    return this.repo.updateItem(orderItemId, data);
  }
}
