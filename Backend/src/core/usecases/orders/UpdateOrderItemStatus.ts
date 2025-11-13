import type { IOrderRepository } from "../../domain/repositories/IOrderRepository";
export class UpdateOrderItemStatus {
  constructor(private repo: IOrderRepository) {}
  exec(orderItemId: number, status: Parameters<IOrderRepository["updateItemStatus"]>[1]) {
    return this.repo.updateItemStatus(orderItemId, status);
  }
}
