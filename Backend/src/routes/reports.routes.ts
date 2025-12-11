import { Router } from "express";
import { asyncHandler } from "../presentation/utils/asyncHandler";
import { authenticate } from "../presentation/middlewares/authenticate";
import { authorize } from "../presentation/middlewares/authorize";
import { reportsController } from "../container";

const router = Router();

router.use(authenticate);

router.get(
  "/payments",
  authorize("expenses.read"),
  asyncHandler(reportsController.payments)
);

router.get(
  "/profit-loss",
  authorize("expenses.read"),
  asyncHandler(reportsController.profitLoss)
);

export default router;
