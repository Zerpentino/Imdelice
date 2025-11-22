import type { CreateInventoryMovementInput, IInventoryRepository } from "../../domain/repositories/IInventoryRepository";
export declare class CreateInventoryMovement {
    private repo;
    constructor(repo: IInventoryRepository);
    exec(input: CreateInventoryMovementInput): Promise<{
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
//# sourceMappingURL=CreateInventoryMovement.d.ts.map