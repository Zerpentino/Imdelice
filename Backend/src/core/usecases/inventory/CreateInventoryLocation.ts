import type { IInventoryRepository } from "../../domain/repositories/IInventoryRepository";
import type { InventoryLocationType } from "@prisma/client";

export class CreateInventoryLocation {
  constructor(private repo: IInventoryRepository) {}

  exec(input: {
    name: string;
    code?: string | null;
    type?: InventoryLocationType;
    isDefault?: boolean;
  }) {
    return this.repo.createLocation(input);
  }
}
