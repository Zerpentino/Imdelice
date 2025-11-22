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
import type { AuthRequest } from "../middlewares/authenticate";
export declare class InventoryController {
    private listItemsUC;
    private getItemUC;
    private listMovementsUC;
    private createMovementUC;
    private createMovementByBarcodeUC;
    private listLocationsUC;
    private createLocationUC;
    private updateLocationUC;
    private deleteLocationUC;
    constructor(listItemsUC: ListInventoryItems, getItemUC: GetInventoryItem, listMovementsUC: ListInventoryMovements, createMovementUC: CreateInventoryMovement, createMovementByBarcodeUC: CreateInventoryMovementByBarcode, listLocationsUC: ListInventoryLocations, createLocationUC: CreateInventoryLocation, updateLocationUC: UpdateInventoryLocation, deleteLocationUC: DeleteInventoryLocation);
    listItems: (req: AuthRequest, res: Response) => Promise<Response<any, Record<string, any>>>;
    getItem: (req: AuthRequest, res: Response) => Promise<Response<any, Record<string, any>>>;
    listItemMovements: (req: AuthRequest, res: Response) => Promise<Response<any, Record<string, any>>>;
    createMovement: (req: AuthRequest, res: Response) => Promise<Response<any, Record<string, any>>>;
    createMovementByBarcode: (req: AuthRequest, res: Response) => Promise<Response<any, Record<string, any>>>;
    listLocations: (_req: AuthRequest, res: Response) => Promise<Response<any, Record<string, any>>>;
    createLocation: (req: AuthRequest, res: Response) => Promise<Response<any, Record<string, any>>>;
    updateLocation: (req: AuthRequest, res: Response) => Promise<Response<any, Record<string, any>>>;
    deleteLocation: (req: AuthRequest, res: Response) => Promise<Response<any, Record<string, any>>>;
}
//# sourceMappingURL=InventoryController.d.ts.map