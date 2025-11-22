import type { IInventoryRepository } from "../../domain/repositories/IInventoryRepository";

export class ListInventoryLocations {
  constructor(private repo: IInventoryRepository) {}

  exec() {
    return this.repo.listLocations();
  }
}
