import type {
  IOrderRepository,
  ListOrdersFilters,
  ListOrdersItem,
} from "../../domain/repositories/IOrderRepository";

export class ListOrders {
  constructor(private readonly repo: IOrderRepository) {}

  exec(filters: ListOrdersFilters): Promise<ListOrdersItem[]> {
    return this.repo.list(filters);
  }
}
