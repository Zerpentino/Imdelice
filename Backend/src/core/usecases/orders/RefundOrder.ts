import type { IOrderRepository } from "../../domain/repositories/IOrderRepository";

export class RefundOrder {
  constructor(private readonly repo: IOrderRepository) {}

  exec(orderId: number, input: { adminUserId: number; reason?: string | null }) {
    return this.repo.refundOrder(orderId, input);
  }
}
