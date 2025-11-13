import type { IOrderRepository } from "../../domain/repositories/IOrderRepository";

export class RemoveOrderItem {
  constructor(private repo: IOrderRepository) {}
  exec(orderItemId: number) {
    return this.repo.removeItem(orderItemId);
  }
}
