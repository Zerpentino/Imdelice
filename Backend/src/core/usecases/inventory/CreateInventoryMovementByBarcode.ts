import type { InventoryMovementType } from "@prisma/client";
import type { IInventoryRepository } from "../../domain/repositories/IInventoryRepository";
import type { IProductRepository } from "../../domain/repositories/IProductRepository";

interface Input {
  barcode: string;
  type: InventoryMovementType;
  quantity: number;
  locationId?: number | null;
  reason?: string | null;
  userId?: number | null;
}

export class CreateInventoryMovementByBarcode {
  constructor(
    private inventoryRepo: IInventoryRepository,
    private productRepo: IProductRepository,
  ) {}

  async exec(input: Input) {
    const normalized = input.barcode?.trim();
    if (!normalized) throw new Error("Barcode requerido");

    const product = await this.productRepo.findByBarcode(normalized);
    if (!product) throw new Error("Producto no encontrado para ese código");

    const variant = product.variants.find((v) => v.barcode === normalized);

    return this.inventoryRepo.createMovement({
      productId: product.id,
      variantId: variant ? variant.id : null,
      locationId: input.locationId ?? null,
      type: input.type,
      quantity: input.quantity,
      reason: input.reason,
      performedByUserId: input.userId ?? null,
    });
  }
}
