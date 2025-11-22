import type {
  IInventoryRepository,
  ListInventoryMovementsInput,
} from "../../domain/repositories/IInventoryRepository";

export class ListInventoryMovements {
  constructor(private repo: IInventoryRepository) {}

  exec(filter?: ListInventoryMovementsInput) {
    return this.repo.listMovements(filter);
  }
}
