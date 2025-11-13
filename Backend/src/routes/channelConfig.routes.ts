import { Router } from "express";
import { authenticate } from "../presentation/middlewares/authenticate";
import { authorize } from "../presentation/middlewares/authorize";
import { asyncHandler } from "../presentation/utils/asyncHandler";
import { channelConfigController } from "../container";

const router = Router();

router.use(authenticate);

router.get("/", authorize("orders.update"), asyncHandler(channelConfigController.list));
router.put(
  "/:source",
  authorize("orders.update"),
  asyncHandler(channelConfigController.set)
);

export default router;
