import { Prisma, InventoryMovementType, InventoryLocationType } from "@prisma/client";
import type { CreateInventoryMovementInput, IInventoryRepository, InventoryItemFilter, ListInventoryItemsInput, ListInventoryMovementsInput } from "../../core/domain/repositories/IInventoryRepository";
export declare class PrismaInventoryRepository implements IInventoryRepository {
    getDefaultLocation(): Promise<{
        name: string;
        id: number;
        createdAt: Date;
        updatedAt: Date;
        code: string | null;
        type: import(".prisma/client").$Enums.InventoryLocationType;
        isActive: boolean;
        isDefault: boolean;
    } | null>;
    ensureDefaultLocation(): Promise<{
        name: string;
        id: number;
        createdAt: Date;
        updatedAt: Date;
        code: string | null;
        type: import(".prisma/client").$Enums.InventoryLocationType;
        isActive: boolean;
        isDefault: boolean;
    }>;
    findItem(filter: InventoryItemFilter): Promise<({
        product: {
            name: string;
            id: number;
            description: string | null;
            createdAt: Date;
            updatedAt: Date;
            category: {
                name: string;
                id: number;
                slug: string;
            };
            type: import(".prisma/client").$Enums.ProductType;
            isActive: boolean;
            categoryId: number;
            priceCents: number | null;
            sku: string | null;
            barcode: string | null;
            imageMimeType: string | null;
            imageSize: number | null;
            isAvailable: boolean;
        };
        variant: {
            name: string;
            id: number;
            position: number;
            isActive: boolean;
            priceCents: number;
            sku: string | null;
            barcode: string | null;
            isAvailable: boolean;
            productId: number;
        } | null;
        location: {
            name: string;
            id: number;
            createdAt: Date;
            updatedAt: Date;
            code: string | null;
            type: import(".prisma/client").$Enums.InventoryLocationType;
            isActive: boolean;
            isDefault: boolean;
        };
    } & {
        id: number;
        createdAt: Date;
        updatedAt: Date;
        productId: number;
        variantId: number | null;
        currentQuantity: Prisma.Decimal;
        unit: import(".prisma/client").$Enums.UnitOfMeasure;
        minThreshold: Prisma.Decimal | null;
        maxThreshold: Prisma.Decimal | null;
        lastMovementAt: Date | null;
        locationId: number;
    }) | null>;
    listItems(filter?: ListInventoryItemsInput): Promise<({
        product: {
            name: string;
            id: number;
            description: string | null;
            createdAt: Date;
            updatedAt: Date;
            category: {
                name: string;
                id: number;
                slug: string;
            };
            type: import(".prisma/client").$Enums.ProductType;
            isActive: boolean;
            categoryId: number;
            priceCents: number | null;
            sku: string | null;
            barcode: string | null;
            imageMimeType: string | null;
            imageSize: number | null;
            isAvailable: boolean;
        };
        variant: {
            name: string;
            id: number;
            position: number;
            isActive: boolean;
            priceCents: number;
            sku: string | null;
            barcode: string | null;
            isAvailable: boolean;
            productId: number;
        } | null;
        location: {
            name: string;
            id: number;
            createdAt: Date;
            updatedAt: Date;
            code: string | null;
            type: import(".prisma/client").$Enums.InventoryLocationType;
            isActive: boolean;
            isDefault: boolean;
        };
    } & {
        id: number;
        createdAt: Date;
        updatedAt: Date;
        productId: number;
        variantId: number | null;
        currentQuantity: Prisma.Decimal;
        unit: import(".prisma/client").$Enums.UnitOfMeasure;
        minThreshold: Prisma.Decimal | null;
        maxThreshold: Prisma.Decimal | null;
        lastMovementAt: Date | null;
        locationId: number;
    })[]>;
    listMovements(filter?: ListInventoryMovementsInput): Promise<({
        product: {
            name: string;
            id: number;
            sku: string | null;
            barcode: string | null;
        };
        variant: {
            name: string;
            id: number;
            sku: string | null;
            barcode: string | null;
        } | null;
        location: {
            name: string;
            id: number;
            createdAt: Date;
            updatedAt: Date;
            code: string | null;
            type: import(".prisma/client").$Enums.InventoryLocationType;
            isActive: boolean;
            isDefault: boolean;
        } | null;
    } & {
        id: number;
        createdAt: Date;
        type: import(".prisma/client").$Enums.InventoryMovementType;
        productId: number;
        variantId: number | null;
        quantity: Prisma.Decimal;
        unit: import(".prisma/client").$Enums.UnitOfMeasure;
        locationId: number | null;
        reason: string | null;
        metadata: Prisma.JsonValue | null;
        relatedOrderId: number | null;
        performedByUserId: number | null;
    })[]>;
    hasOrderMovement(orderId: number, type: InventoryMovementType): Promise<boolean>;
    createMovement(input: CreateInventoryMovementInput): Promise<{
        id: number;
        createdAt: Date;
        type: import(".prisma/client").$Enums.InventoryMovementType;
        productId: number;
        variantId: number | null;
        quantity: Prisma.Decimal;
        unit: import(".prisma/client").$Enums.UnitOfMeasure;
        locationId: number | null;
        reason: string | null;
        metadata: Prisma.JsonValue | null;
        relatedOrderId: number | null;
        performedByUserId: number | null;
    }>;
    private resolveLocationId;
    listLocations(): Promise<{
        name: string;
        id: number;
        createdAt: Date;
        updatedAt: Date;
        code: string | null;
        type: import(".prisma/client").$Enums.InventoryLocationType;
        isActive: boolean;
        isDefault: boolean;
    }[]>;
    createLocation(data: {
        name: string;
        code?: string | null;
        type?: InventoryLocationType;
        isDefault?: boolean;
    }): Promise<{
        name: string;
        id: number;
        createdAt: Date;
        updatedAt: Date;
        code: string | null;
        type: import(".prisma/client").$Enums.InventoryLocationType;
        isActive: boolean;
        isDefault: boolean;
    }>;
    updateLocation(id: number, data: {
        name?: string;
        code?: string | null;
        type?: InventoryLocationType;
        isDefault?: boolean;
        isActive?: boolean;
    }): Promise<{
        name: string;
        id: number;
        createdAt: Date;
        updatedAt: Date;
        code: string | null;
        type: import(".prisma/client").$Enums.InventoryLocationType;
        isActive: boolean;
        isDefault: boolean;
    }>;
    deleteLocation(id: number): Promise<void>;
    private attachProductImageMeta;
}
//# sourceMappingURL=PrismaInventoryRepository.d.ts.map