import type { InventoryLocationType } from "@prisma/client";
import type { IInventoryRepository } from "../../domain/repositories/IInventoryRepository";

export class UpdateInventoryLocation {
  constructor(private repo: IInventoryRepository) {}

  exec(
    id: number,
    input: {
      name?: string;
      code?: string | null;
      type?: InventoryLocationType;
      isDefault?: boolean;
      isActive?: boolean;
    },
  ) {
    return this.repo.updateLocation(id, input);
  }
}
