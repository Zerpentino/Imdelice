import type { IOrderRepository } from "../../domain/repositories/IOrderRepository";
export class AddPayment {
  constructor(private repo: IOrderRepository) {}
  exec(orderId: number, data: Parameters<IOrderRepository["addPayment"]>[1]) {
    return this.repo.addPayment(orderId, data);
  }
}
