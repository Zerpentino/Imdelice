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
export declare class CreateInventoryMovementByBarcode {
    private inventoryRepo;
    private productRepo;
    constructor(inventoryRepo: IInventoryRepository, productRepo: IProductRepository);
    exec(input: Input): Promise<{
        id: number;
        createdAt: Date;
        type: import(".prisma/client").$Enums.InventoryMovementType;
        productId: number;
        variantId: number | null;
        quantity: import("@prisma/client/runtime/library").Decimal;
        unit: import(".prisma/client").$Enums.UnitOfMeasure;
        locationId: number | null;
        reason: string | null;
        metadata: import("@prisma/client/runtime/library").JsonValue | null;
        relatedOrderId: number | null;
        performedByUserId: number | null;
    }>;
}
export {};
//# sourceMappingURL=CreateInventoryMovementByBarcode.d.ts.map