import { Router } from "express";
import { asyncHandler } from "../presentation/utils/asyncHandler";
import { authenticate } from "../presentation/middlewares/authenticate";
import { modifiersController } from "../container";
import { authorize } from "../presentation/middlewares/authorize";

const router = Router();
router.use(authenticate);

router.post("/groups", authorize('modifiers.create'), asyncHandler(modifiersController.createGroup));
router.patch("/groups/:id", authorize('modifiers.update'), asyncHandler(modifiersController.updateGroup));
router.delete("/groups/:id", authorize('modifiers.delete'), asyncHandler(modifiersController.removeGroup)); // ?hard=true
router.patch('/modifier-options/:optionId', authorize('modifiers.update'), modifiersController.updateOption);

// LISTADOS
router.get("/groups", authorize('modifiers.read'), asyncHandler(modifiersController.listGroups));
router.get("/groups/by-product/:productId", authorize('modifiers.read'), asyncHandler(modifiersController.listByProduct)); // poner antes de :id
router.get("/groups/:id", authorize('modifiers.read'), asyncHandler(modifiersController.getGroup));
router.get("/groups/:groupId/products", authorize('modifiers.read'), asyncHandler(modifiersController.listProductsByGroup));

export default router;
