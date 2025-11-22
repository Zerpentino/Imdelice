import type { IInventoryRepository, ListInventoryItemsInput } from "../../domain/repositories/IInventoryRepository";
export declare class ListInventoryItems {
    private repo;
    constructor(repo: IInventoryRepository);
    exec(filter?: ListInventoryItemsInput): Promise<{
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
    }[]>;
}
//# sourceMappingURL=ListInventoryItems.d.ts.map