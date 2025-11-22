import { Router } from "express";
import { authenticate } from "../presentation/middlewares/authenticate";
import { authorize } from "../presentation/middlewares/authorize";
import { asyncHandler } from "../presentation/utils/asyncHandler";
import { expensesController } from "../container";

const router = Router();
router.use(authenticate);

router.get("/", authorize("expenses.read"), asyncHandler(expensesController.list));
router.get("/summary", authorize("expenses.read"), asyncHandler(expensesController.summary));
router.get("/:id", authorize("expenses.read"), asyncHandler(expensesController.get));
router.post("/", authorize("expenses.manage"), asyncHandler(expensesController.create));
router.patch("/:id", authorize("expenses.manage"), asyncHandler(expensesController.update));
router.delete("/:id", authorize("expenses.manage"), asyncHandler(expensesController.remove));

export default router;
