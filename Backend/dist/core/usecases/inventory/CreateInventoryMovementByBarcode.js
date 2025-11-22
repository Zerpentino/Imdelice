"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.CreateInventoryMovementByBarcode = void 0;
class CreateInventoryMovementByBarcode {
    constructor(inventoryRepo, productRepo) {
        this.inventoryRepo = inventoryRepo;
        this.productRepo = productRepo;
    }
    async exec(input) {
        const normalized = input.barcode?.trim();
        if (!normalized)
            throw new Error("Barcode requerido");
        const product = await this.productRepo.findByBarcode(normalized);
        if (!product)
            throw new Error("Producto no encontrado para ese código");
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
exports.CreateInventoryMovementByBarcode = CreateInventoryMovementByBarcode;
//# sourceMappingURL=CreateInventoryMovementByBarcode.js.map