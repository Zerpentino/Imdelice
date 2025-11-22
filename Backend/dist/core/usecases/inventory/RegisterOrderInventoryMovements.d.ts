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
export declare class RegisterOrderInventoryMovements {
    private inventoryRepo;
    constructor(inventoryRepo: IInventoryRepository);
    exec({ order, movementType, userId, options }: Input): Promise<void>;
    private collectLeaves;
    private getSignedQuantity;
}
export {};
//# sourceMappingURL=RegisterOrderInventoryMovements.d.ts.map