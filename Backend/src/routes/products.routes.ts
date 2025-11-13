import { Router } from "express";
import { asyncHandler } from "../presentation/utils/asyncHandler";
import { authenticate } from "../presentation/middlewares/authenticate";
import { productsController } from "../container";
import { uploadImage } from "../presentation/middlewares/uploadImage";
import { authorize } from "../presentation/middlewares/authorize";

const router = Router();
router.use(authenticate);

router.post("/simple",    authorize('products.create'), uploadImage.single('image'), asyncHandler(productsController.createSimple));
router.post("/varianted", authorize('products.create'), uploadImage.single('image'), asyncHandler(productsController.createVarianted));

router.get("/",           authorize('products.read'), asyncHandler(productsController.list));
router.get("/variants/:variantId/modifier-groups", authorize('products.read'), asyncHandler(productsController.listVariantModifierGroups));
router.post("/variants/:variantId/modifier-groups", authorize('products.update'), asyncHandler(productsController.attachModifierToVariant));
router.patch("/variants/:variantId/modifier-groups/:groupId", authorize('products.update'), asyncHandler(productsController.updateVariantModifierGroup));
router.delete("/variants/:variantId/modifier-groups/:groupId", authorize('products.update'), asyncHandler(productsController.detachModifierFromVariant));
router.get("/:id",        authorize('products.read'), asyncHandler(productsController.getDetail));
router.post("/attach-modifier", authorize('products.update'), asyncHandler(productsController.attachModifier));
router.post('/detach-modifier', authorize('products.update'), asyncHandler(productsController.detachModifierGroup)); // 👈 NUEVA
router.post('/modifier-position', authorize('products.update'), asyncHandler(productsController.updateModifierGroupPosition)); // 👈 NUEVA
router.post('/modifier-reorder', authorize('products.update'), asyncHandler(productsController.reorderModifierGroups)); // 👈 OPCIONAL


router.patch("/:id", authorize('products.update'), uploadImage.single('image'), asyncHandler(productsController.update));
router.put("/:id/variants", authorize('products.update'), asyncHandler(productsController.replaceVariants));
router.delete("/:id", authorize('products.delete'), asyncHandler(productsController.remove)); // ?hard=true
// Imagen binaria (para que el front la muestre)
router.get("/:id/image", authorize('products.read'), asyncHandler(productsController.getImage));

// Conversión SIMPLE → VARIANTED y VARIANTED → SIMPLE
router.post("/:id/convert-to-varianted", authorize('products.update'), asyncHandler(productsController.convertToVarianted));
router.post("/:id/convert-to-simple",    authorize('products.update'), asyncHandler(productsController.convertToSimple));



//COMBOSSSSS
router.post("/combo", authorize('products.create'), uploadImage.single('image'), asyncHandler(productsController.createCombo));
router.post("/:id/combo-items", authorize('products.update'), asyncHandler(productsController.addComboItems));
router.patch("/combo-items/:comboItemId", authorize('products.update'), asyncHandler(productsController.updateComboItem));
router.delete("/combo-items/:comboItemId", authorize('products.update'), asyncHandler(productsController.removeComboItem));

export default router;
