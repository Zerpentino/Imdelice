import type { IOrderRepository } from "../../domain/repositories/IOrderRepository";
export class AddOrderItem {
  constructor(private repo: IOrderRepository) {}
  exec(orderId: number, data: Parameters<IOrderRepository["addItem"]>[1]) {
    return this.repo.addItem(orderId, data);
  }
}
