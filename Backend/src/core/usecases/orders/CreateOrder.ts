import type { IOrderRepository } from "../../domain/repositories/IOrderRepository";
export class CreateOrder {
  constructor(private repo: IOrderRepository) {}
  exec(input: Parameters<IOrderRepository["create"]>[0], servedByUserId: number) {
    return this.repo.create(input, servedByUserId);
  }
}
