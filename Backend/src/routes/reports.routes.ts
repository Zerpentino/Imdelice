import { Router } from "express";
import { asyncHandler } from "../presentation/utils/asyncHandler";
import { authenticate } from "../presentation/middlewares/authenticate";
import { authorize } from "../presentation/middlewares/authorize";
import { reportsController } from "../container";

const router = Router();

router.use(authenticate);

router.get(
  "/payments",
  authorize("orders.read"),
  asyncHandler(reportsController.payments)
);

export default router;
