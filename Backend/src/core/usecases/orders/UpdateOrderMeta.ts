import type { IOrderRepository } from "../../domain/repositories/IOrderRepository";

export class UpdateOrderMeta {
  constructor(private repo: IOrderRepository) {}
  exec(orderId: number, data: {
    tableId?: number | null;
    note?: string | null;
    covers?: number | null;
    prepEtaMinutes?: number | null;
  }) {
    return this.repo.updateMeta(orderId, data);
  }
}
