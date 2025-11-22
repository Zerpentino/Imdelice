"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.RegisterOrderInventoryMovements = void 0;
class RegisterOrderInventoryMovements {
    constructor(inventoryRepo) {
        this.inventoryRepo = inventoryRepo;
    }
    async exec({ order, movementType, userId, options }) {
        if (!order?.items?.length)
            return;
        if (!options?.force) {
            const alreadyRegistered = await this.inventoryRepo.hasOrderMovement(order.id, movementType);
            if (alreadyRegistered)
                return;
        }
        if (movementType === "SALE_RETURN" &&
            options?.requireSaleToExist) {
            const saleExists = await this.inventoryRepo.hasOrderMovement(order.id, "SALE");
            if (!saleExists)
                return;
        }
        const leaves = this.collectLeaves(order.items);
        if (!leaves.length)
            return;
        for (const item of leaves) {
            const qty = Number(item.quantity ?? 0);
            if (!qty)
                continue;
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
    collectLeaves(items) {
        const roots = items.filter((item) => !item.parentItemId);
        const source = roots.length ? roots : items;
        const leaves = [];
        const visit = (node) => {
            if (node.childItems && node.childItems.length > 0) {
                node.childItems.forEach(visit);
            }
            else {
                leaves.push(node);
            }
        };
        source.forEach(visit);
        return leaves;
    }
    getSignedQuantity(quantity, movementType) {
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
exports.RegisterOrderInventoryMovements = RegisterOrderInventoryMovements;
//# sourceMappingURL=RegisterOrderInventoryMovements.js.map