import type {
  IInventoryRepository,
  ListInventoryItemsInput,
} from "../../domain/repositories/IInventoryRepository";

export class ListInventoryItems {
  constructor(private repo: IInventoryRepository) {}

  exec(filter?: ListInventoryItemsInput) {
    return this.repo.listItems(filter);
  }
}
