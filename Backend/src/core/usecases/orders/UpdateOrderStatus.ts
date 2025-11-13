import type { IOrderRepository, OrderStatus } from "../../domain/repositories/IOrderRepository";

export class UpdateOrderStatus {
  constructor(private readonly repo: IOrderRepository) {}

  exec(orderId: number, status: OrderStatus) {
    return this.repo.updateStatus(orderId, status);
  }
}
