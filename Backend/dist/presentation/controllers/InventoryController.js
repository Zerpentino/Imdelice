"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.InventoryController = void 0;
const inventory_dto_1 = require("../dtos/inventory.dto");
const inventory_location_dto_1 = require("../dtos/inventory.location.dto");
const apiResponse_1 = require("../utils/apiResponse");
class InventoryController {
    constructor(listItemsUC, getItemUC, listMovementsUC, createMovementUC, createMovementByBarcodeUC, listLocationsUC, createLocationUC, updateLocationUC, deleteLocationUC) {
        this.listItemsUC = listItemsUC;
        this.getItemUC = getItemUC;
        this.listMovementsUC = listMovementsUC;
        this.createMovementUC = createMovementUC;
        this.createMovementByBarcodeUC = createMovementByBarcodeUC;
        this.listLocationsUC = listLocationsUC;
        this.createLocationUC = createLocationUC;
        this.updateLocationUC = updateLocationUC;
        this.deleteLocationUC = deleteLocationUC;
        this.listItems = async (req, res) => {
            try {
                const filters = inventory_dto_1.ListInventoryItemsQueryDto.parse(req.query);
                const items = await this.listItemsUC.exec(filters);
                return (0, apiResponse_1.success)(res, items);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error listing inventory", 400, err);
            }
        };
        this.getItem = async (req, res) => {
            try {
                const id = Number(req.params.id);
                if (!Number.isFinite(id) || id <= 0)
                    return (0, apiResponse_1.fail)(res, "Id inválido", 400);
                const item = await this.getItemUC.exec({ id });
                if (!item)
                    return (0, apiResponse_1.fail)(res, "Inventory item not found", 404);
                return (0, apiResponse_1.success)(res, item);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error fetching inventory item", 400, err);
            }
        };
        this.listItemMovements = async (req, res) => {
            try {
                const id = Number(req.params.id);
                if (!Number.isFinite(id) || id <= 0)
                    return (0, apiResponse_1.fail)(res, "Id inválido", 400);
                const item = await this.getItemUC.exec({ id });
                if (!item)
                    return (0, apiResponse_1.fail)(res, "Inventory item not found", 404);
                const filters = inventory_dto_1.InventoryMovementsQueryDto.parse(req.query);
                const movements = await this.listMovementsUC.exec({
                    ...filters,
                    productId: item.productId,
                    variantId: item.variantId,
                    locationId: item.locationId,
                });
                return (0, apiResponse_1.success)(res, movements);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error listing movements", 400, err);
            }
        };
        this.createMovement = async (req, res) => {
            try {
                const dto = inventory_dto_1.InventoryMovementDto.parse(req.body);
                const movement = await this.createMovementUC.exec({
                    ...dto,
                    performedByUserId: req.auth?.userId ?? null,
                });
                return (0, apiResponse_1.success)(res, movement, "Movement recorded", 201);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error recording movement", 400, err);
            }
        };
        this.createMovementByBarcode = async (req, res) => {
            try {
                const dto = inventory_dto_1.InventoryMovementByBarcodeDto.parse(req.body);
                const movement = await this.createMovementByBarcodeUC.exec({
                    ...dto,
                    userId: req.auth?.userId ?? null,
                });
                return (0, apiResponse_1.success)(res, movement, "Movement recorded", 201);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error recording movement", 400, err);
            }
        };
        this.listLocations = async (_req, res) => {
            try {
                const data = await this.listLocationsUC.exec();
                return (0, apiResponse_1.success)(res, data);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error listing locations", 400, err);
            }
        };
        this.createLocation = async (req, res) => {
            try {
                const dto = inventory_location_dto_1.CreateInventoryLocationDto.parse(req.body);
                const location = await this.createLocationUC.exec(dto);
                return (0, apiResponse_1.success)(res, location, "Created", 201);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error creating location", 400, err);
            }
        };
        this.updateLocation = async (req, res) => {
            try {
                const id = Number(req.params.id);
                if (!Number.isFinite(id) || id <= 0)
                    return (0, apiResponse_1.fail)(res, "Id inválido", 400);
                const dto = inventory_location_dto_1.UpdateInventoryLocationDto.parse(req.body);
                const location = await this.updateLocationUC.exec(id, dto);
                return (0, apiResponse_1.success)(res, location, "Updated");
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error updating location", 400, err);
            }
        };
        this.deleteLocation = async (req, res) => {
            try {
                const id = Number(req.params.id);
                if (!Number.isFinite(id) || id <= 0)
                    return (0, apiResponse_1.fail)(res, "Id inválido", 400);
                await this.deleteLocationUC.exec(id);
                return (0, apiResponse_1.success)(res, null, "Deleted", 204);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error deleting location", 400, err);
            }
        };
    }
}
exports.InventoryController = InventoryController;
//# sourceMappingURL=InventoryController.js.map