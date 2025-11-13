import type { IOrderRepository } from "../../domain/repositories/IOrderRepository";
export class GetOrderDetail {
  constructor(private repo: IOrderRepository) {}
  exec(orderId: number) { return this.repo.getById(orderId); }
}
