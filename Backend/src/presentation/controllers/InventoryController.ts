import type { Response } from "express";
import { CreateInventoryMovement } from "../../core/usecases/inventory/CreateInventoryMovement";
import { CreateInventoryMovementByBarcode } from "../../core/usecases/inventory/CreateInventoryMovementByBarcode";
import { GetInventoryItem } from "../../core/usecases/inventory/GetInventoryItem";
import { ListInventoryItems } from "../../core/usecases/inventory/ListInventoryItems";
import { ListInventoryMovements } from "../../core/usecases/inventory/ListInventoryMovements";
import { ListInventoryLocations } from "../../core/usecases/inventory/ListInventoryLocations";
import { CreateInventoryLocation } from "../../core/usecases/inventory/CreateInventoryLocation";
import { UpdateInventoryLocation } from "../../core/usecases/inventory/UpdateInventoryLocation";
import { DeleteInventoryLocation } from "../../core/usecases/inventory/DeleteInventoryLocation";
import {
  InventoryMovementByBarcodeDto,
  InventoryMovementDto,
  InventoryMovementsQueryDto,
  ListInventoryItemsQueryDto,
} from "../dtos/inventory.dto";
import {
  CreateInventoryLocationDto,
  UpdateInventoryLocationDto,
} from "../dtos/inventory.location.dto";
import type { AuthRequest } from "../middlewares/authenticate";
import { fail, success } from "../utils/apiResponse";

export class InventoryController {
  constructor(
    private listItemsUC: ListInventoryItems,
    private getItemUC: GetInventoryItem,
    private listMovementsUC: ListInventoryMovements,
    private createMovementUC: CreateInventoryMovement,
    private createMovementByBarcodeUC: CreateInventoryMovementByBarcode,
    private listLocationsUC: ListInventoryLocations,
    private createLocationUC: CreateInventoryLocation,
    private updateLocationUC: UpdateInventoryLocation,
    private deleteLocationUC: DeleteInventoryLocation,
  ) {}

  listItems = async (req: AuthRequest, res: Response) => {
    try {
      const filters = ListInventoryItemsQueryDto.parse(req.query);
      const items = await this.listItemsUC.exec(filters);
      return success(res, items);
    } catch (err: any) {
      return fail(res, err?.message || "Error listing inventory", 400, err);
    }
  };

  getItem = async (req: AuthRequest, res: Response) => {
    try {
      const id = Number(req.params.id);
      if (!Number.isFinite(id) || id <= 0) return fail(res, "Id inválido", 400);
      const item = await this.getItemUC.exec({ id });
      if (!item) return fail(res, "Inventory item not found", 404);
      return success(res, item);
    } catch (err: any) {
      return fail(res, err?.message || "Error fetching inventory item", 400, err);
    }
  };

  listItemMovements = async (req: AuthRequest, res: Response) => {
    try {
      const id = Number(req.params.id);
      if (!Number.isFinite(id) || id <= 0) return fail(res, "Id inválido", 400);
      const item = await this.getItemUC.exec({ id });
      if (!item) return fail(res, "Inventory item not found", 404);

      const filters = InventoryMovementsQueryDto.parse(req.query);
      const movements = await this.listMovementsUC.exec({
        ...filters,
        productId: item.productId,
        variantId: item.variantId,
        locationId: item.locationId,
      });
      return success(res, movements);
    } catch (err: any) {
      return fail(res, err?.message || "Error listing movements", 400, err);
    }
  };

  createMovement = async (req: AuthRequest, res: Response) => {
    try {
      const dto = InventoryMovementDto.parse(req.body);
      const movement = await this.createMovementUC.exec({
        ...dto,
        performedByUserId: req.auth?.userId ?? null,
      });
      return success(res, movement, "Movement recorded", 201);
    } catch (err: any) {
      return fail(res, err?.message || "Error recording movement", 400, err);
    }
  };

  createMovementByBarcode = async (req: AuthRequest, res: Response) => {
    try {
      const dto = InventoryMovementByBarcodeDto.parse(req.body);
      const movement = await this.createMovementByBarcodeUC.exec({
        ...dto,
        userId: req.auth?.userId ?? null,
      });
      return success(res, movement, "Movement recorded", 201);
    } catch (err: any) {
      return fail(res, err?.message || "Error recording movement", 400, err);
    }
  };

  listLocations = async (_req: AuthRequest, res: Response) => {
    try {
      const data = await this.listLocationsUC.exec();
      return success(res, data);
    } catch (err: any) {
      return fail(res, err?.message || "Error listing locations", 400, err);
    }
  };

  createLocation = async (req: AuthRequest, res: Response) => {
    try {
      const dto = CreateInventoryLocationDto.parse(req.body);
      const location = await this.createLocationUC.exec(dto);
      return success(res, location, "Created", 201);
    } catch (err: any) {
      return fail(res, err?.message || "Error creating location", 400, err);
    }
  };

  updateLocation = async (req: AuthRequest, res: Response) => {
    try {
      const id = Number(req.params.id);
      if (!Number.isFinite(id) || id <= 0) return fail(res, "Id inválido", 400);
      const dto = UpdateInventoryLocationDto.parse(req.body);
      const location = await this.updateLocationUC.exec(id, dto);
      return success(res, location, "Updated");
    } catch (err: any) {
      return fail(res, err?.message || "Error updating location", 400, err);
    }
  };

  deleteLocation = async (req: AuthRequest, res: Response) => {
    try {
      const id = Number(req.params.id);
      if (!Number.isFinite(id) || id <= 0) return fail(res, "Id inválido", 400);
      await this.deleteLocationUC.exec(id);
      return success(res, null, "Deleted", 204);
    } catch (err: any) {
      return fail(res, err?.message || "Error deleting location", 400, err);
    }
  };
}
