import { Router } from "express";
import { asyncHandler } from "../presentation/utils/asyncHandler";
import { authenticate } from "../presentation/middlewares/authenticate";
import { ordersController } from "../container";
import { authorize } from "../presentation/middlewares/authorize";

const router = Router();
router.use(authenticate);

router.get("/", authorize("orders.read"), asyncHandler(ordersController.list));
router.get("/kds/list", authorize("orders.read"), asyncHandler(ordersController.kds));
router.get("/:id", authorize("orders.read"), asyncHandler(ordersController.get));

router.post("/", authorize("orders.create"), asyncHandler(ordersController.create));

router.patch("/:id/meta", authorize("orders.update"), asyncHandler(ordersController.updateMeta));
router.patch("/:id/status", authorize("orders.update"), asyncHandler(ordersController.updateStatus));
router.post("/:id/refund", authorize("orders.update"), asyncHandler(ordersController.refund));

router.post("/:id/items", authorize("orders.update"), asyncHandler(ordersController.addItem));
router.patch("/items/:itemId/status", authorize("orders.update"), asyncHandler(ordersController.setItemStatus));
router.patch("/items/:itemId", authorize("orders.update"), asyncHandler(ordersController.updateItem));
router.delete("/items/:itemId", authorize("orders.update"), asyncHandler(ordersController.removeItem));

router.post("/:id/payments", authorize("orders.update"), asyncHandler(ordersController.addPayment));

router.post("/:id/split", authorize("orders.update"), asyncHandler(ordersController.splitByItems));

export default router;
