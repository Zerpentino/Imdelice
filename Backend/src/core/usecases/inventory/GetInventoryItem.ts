import type {
  IInventoryRepository,
  InventoryItemFilter,
} from "../../domain/repositories/IInventoryRepository";

export class GetInventoryItem {
  constructor(private repo: IInventoryRepository) {}

  exec(filter: InventoryItemFilter) {
    return this.repo.findItem(filter);
  }
}
