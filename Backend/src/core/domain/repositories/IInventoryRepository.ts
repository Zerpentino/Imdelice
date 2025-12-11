import type {
  InventoryItem,
  InventoryLocation,
  InventoryLocationType,
  InventoryMovement,
  InventoryMovementType,
  UnitOfMeasure,
} from "@prisma/client";

export interface InventoryItemFilter {
  id?: number;
  productId?: number;
  variantId?: number | null;
  locationId?: number;
}

export interface ListInventoryItemsInput extends InventoryItemFilter {
  categoryId?: number;
  search?: string;
  trackInventory?: boolean;
}

export interface ListInventoryMovementsInput {
  productId?: number;
  variantId?: number | null;
  locationId?: number;
  orderId?: number;
  type?: InventoryMovementType;
  limit?: number;
}

export interface CreateInventoryMovementInput {
  productId: number;
  variantId?: number | null;
  locationId?: number | null;
  type: InventoryMovementType;
  quantity: number;
  unit?: UnitOfMeasure;
  reason?: string | null;
  relatedOrderId?: number | null;
  performedByUserId?: number | null;
  metadata?: Record<string, any> | null;
}

export interface IInventoryRepository {
  getDefaultLocation(): Promise<InventoryLocation | null>;
  ensureDefaultLocation(): Promise<InventoryLocation>;
  findItem(filter: InventoryItemFilter): Promise<InventoryItem | null>;
  listItems(filter?: ListInventoryItemsInput): Promise<InventoryItem[]>;
  listMovements(filter?: ListInventoryMovementsInput): Promise<InventoryMovement[]>;
  createMovement(input: CreateInventoryMovementInput): Promise<InventoryMovement | null>;
  hasOrderMovement(orderId: number, type: InventoryMovementType): Promise<boolean>;
  listLocations(): Promise<InventoryLocation[]>;
  createLocation(data: {
    name: string;
    code?: string | null;
    type?: InventoryLocationType;
    isDefault?: boolean;
  }): Promise<InventoryLocation>;
  updateLocation(
    id: number,
    data: {
      name?: string;
      code?: string | null;
      type?: InventoryLocationType;
      isDefault?: boolean;
      isActive?: boolean;
    },
  ): Promise<InventoryLocation>;
  deleteLocation(id: number): Promise<void>;
}
