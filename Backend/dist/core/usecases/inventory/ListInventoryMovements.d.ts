import type { IInventoryRepository, ListInventoryMovementsInput } from "../../domain/repositories/IInventoryRepository";
export declare class ListInventoryMovements {
    private repo;
    constructor(repo: IInventoryRepository);
    exec(filter?: ListInventoryMovementsInput): Promise<{
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
    }[]>;
}
//# sourceMappingURL=ListInventoryMovements.d.ts.map