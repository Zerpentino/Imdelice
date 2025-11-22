import type { InventoryMovementType } from "@prisma/client";
import type { IInventoryRepository } from "../../domain/repositories/IInventoryRepository";

type OrderItemNode = {
  id: number;
  productId: number;
  variantId: number | null;
  quantity: number;
  parentItemId: number | null;
  childItems?: OrderItemNode[];
};

type OrderWithItems = {
  id: number;
  items: OrderItemNode[];
};

interface ExecOptions {
  force?: boolean;
  requireSaleToExist?: boolean;
}

interface Input {
  order: OrderWithItems;
  movementType: InventoryMovementType;
  userId?: number | null;
  options?: ExecOptions;
}

export class RegisterOrderInventoryMovements {
  constructor(private inventoryRepo: IInventoryRepository) {}

  async exec({ order, movementType, userId, options }: Input) {
    if (!order?.items?.length) return;

    if (!options?.force) {
      const alreadyRegistered = await this.inventoryRepo.hasOrderMovement(
        order.id,
        movementType,
      );
      if (alreadyRegistered) return;
    }

    if (
      movementType === "SALE_RETURN" &&
      options?.requireSaleToExist
    ) {
      const saleExists = await this.inventoryRepo.hasOrderMovement(
        order.id,
        "SALE",
      );
      if (!saleExists) return;
    }

    const leaves = this.collectLeaves(order.items);
    if (!leaves.length) return;

    for (const item of leaves) {
      const qty = Number(item.quantity ?? 0);
      if (!qty) continue;
      await this.inventoryRepo.createMovement({
        productId: item.productId,
        variantId: item.variantId ?? null,
        type: movementType,
        quantity: this.getSignedQuantity(qty, movementType),
        relatedOrderId: order.id,
        performedByUserId: userId ?? null,
      });
    }
  }

  private collectLeaves(items: OrderItemNode[]) {
    const roots = items.filter((item) => !item.parentItemId);
    const source = roots.length ? roots : items;
    const leaves: OrderItemNode[] = [];

    const visit = (node: OrderItemNode) => {
      if (node.childItems && node.childItems.length > 0) {
        node.childItems.forEach(visit);
      } else {
        leaves.push(node);
      }
    };

    source.forEach(visit);
    return leaves;
  }

  private getSignedQuantity(
    quantity: number,
    movementType: InventoryMovementType,
  ) {
    const absolute = Math.abs(quantity);
    switch (movementType) {
      case "SALE":
        return -absolute;
      case "SALE_RETURN":
        return absolute;
      default:
        return quantity;
    }
  }
}
