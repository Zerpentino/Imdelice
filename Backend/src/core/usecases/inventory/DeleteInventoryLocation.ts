import type { IInventoryRepository } from "../../domain/repositories/IInventoryRepository";

export class DeleteInventoryLocation {
  constructor(private repo: IInventoryRepository) {}

  exec(id: number) {
    return this.repo.deleteLocation(id);
  }
}
