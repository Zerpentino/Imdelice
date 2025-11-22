import type { IInventoryRepository, InventoryItemFilter } from "../../domain/repositories/IInventoryRepository";
export declare class GetInventoryItem {
    private repo;
    constructor(repo: IInventoryRepository);
    exec(filter: InventoryItemFilter): Promise<{
        id: number;
        createdAt: Date;
        updatedAt: Date;
        productId: number;
        variantId: number | null;
        currentQuantity: import("@prisma/client/runtime/library").Decimal;
        unit: import(".prisma/client").$Enums.UnitOfMeasure;
        minThreshold: import("@prisma/client/runtime/library").Decimal | null;
        maxThreshold: import("@prisma/client/runtime/library").Decimal | null;
        lastMovementAt: Date | null;
        locationId: number;
    } | null>;
}
//# sourceMappingURL=GetInventoryItem.d.ts.map