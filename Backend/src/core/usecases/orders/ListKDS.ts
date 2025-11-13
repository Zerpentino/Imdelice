import type { IOrderRepository, KDSFilters } from "../../domain/repositories/IOrderRepository";
export class ListKDS {
  constructor(private repo: IOrderRepository) {}
  exec(filters: KDSFilters) { return this.repo.listKDS(filters); }
}
