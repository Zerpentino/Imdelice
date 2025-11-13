import type { IOrderRepository } from "../../domain/repositories/IOrderRepository";

export class SplitOrderByItems {
  constructor(private repo: IOrderRepository) {}
  exec(sourceOrderId: number, input: {
    itemIds: number[];
    servedByUserId: number;
    serviceType?: "DINE_IN"|"TAKEAWAY"|"DELIVERY";
    tableId?: number | null;
    note?: string | null;
    covers?: number | null;
  }) {
    return this.repo.splitOrderByItems(sourceOrderId, input);
  }
}
