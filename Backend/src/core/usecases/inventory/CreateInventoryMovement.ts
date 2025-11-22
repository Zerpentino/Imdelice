import type {
  CreateInventoryMovementInput,
  IInventoryRepository,
} from "../../domain/repositories/IInventoryRepository";

export class CreateInventoryMovement {
  constructor(private repo: IInventoryRepository) {}

  exec(input: CreateInventoryMovementInput) {
    return this.repo.createMovement(input);
  }
}
