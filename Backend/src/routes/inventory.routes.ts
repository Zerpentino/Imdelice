import { Router } from "express";
import { inventoryController } from "../container";
import { asyncHandler } from "../presentation/utils/asyncHandler";
import { authenticate } from "../presentation/middlewares/authenticate";
import { authorize } from "../presentation/middlewares/authorize";

const router = Router();
router.use(authenticate);

router.get(
  "/items",
  authorize("inventory.read"),
  asyncHandler(inventoryController.listItems),
);

router.get(
  "/items/:id",
  authorize("inventory.read"),
  asyncHandler(inventoryController.getItem),
);

router.get(
  "/items/:id/movements",
  authorize("inventory.read"),
  asyncHandler(inventoryController.listItemMovements),
);

router.post(
  "/movements",
  authorize("inventory.adjust"),
  asyncHandler(inventoryController.createMovement),
);

router.post(
  "/movements/by-barcode",
  authorize("inventory.adjust"),
  asyncHandler(inventoryController.createMovementByBarcode),
);

router.get(
  "/locations",
  authorize("inventory.read"),
  asyncHandler(inventoryController.listLocations),
);

router.post(
  "/locations",
  authorize("inventory.adjust"),
  asyncHandler(inventoryController.createLocation),
);

router.patch(
  "/locations/:id",
  authorize("inventory.adjust"),
  asyncHandler(inventoryController.updateLocation),
);

router.delete(
  "/locations/:id",
  authorize("inventory.adjust"),
  asyncHandler(inventoryController.deleteLocation),
);

export default router;
