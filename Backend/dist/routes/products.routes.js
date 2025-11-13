"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const express_1 = require("express");
const asyncHandler_1 = require("../presentation/utils/asyncHandler");
const authenticate_1 = require("../presentation/middlewares/authenticate");
const container_1 = require("../container");
const uploadImage_1 = require("../presentation/middlewares/uploadImage");
const authorize_1 = require("../presentation/middlewares/authorize");
const router = (0, express_1.Router)();
router.use(authenticate_1.authenticate);
router.post("/simple", (0, authorize_1.authorize)('products.create'), uploadImage_1.uploadImage.single('image'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.createSimple));
router.post("/varianted", (0, authorize_1.authorize)('products.create'), uploadImage_1.uploadImage.single('image'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.createVarianted));
router.get("/", (0, authorize_1.authorize)('products.read'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.list));
router.get("/variants/:variantId/modifier-groups", (0, authorize_1.authorize)('products.read'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.listVariantModifierGroups));
router.post("/variants/:variantId/modifier-groups", (0, authorize_1.authorize)('products.update'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.attachModifierToVariant));
router.patch("/variants/:variantId/modifier-groups/:groupId", (0, authorize_1.authorize)('products.update'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.updateVariantModifierGroup));
router.delete("/variants/:variantId/modifier-groups/:groupId", (0, authorize_1.authorize)('products.update'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.detachModifierFromVariant));
router.get("/:id", (0, authorize_1.authorize)('products.read'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.getDetail));
router.post("/attach-modifier", (0, authorize_1.authorize)('products.update'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.attachModifier));
router.post('/detach-modifier', (0, authorize_1.authorize)('products.update'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.detachModifierGroup)); // 👈 NUEVA
router.post('/modifier-position', (0, authorize_1.authorize)('products.update'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.updateModifierGroupPosition)); // 👈 NUEVA
router.post('/modifier-reorder', (0, authorize_1.authorize)('products.update'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.reorderModifierGroups)); // 👈 OPCIONAL
router.patch("/:id", (0, authorize_1.authorize)('products.update'), uploadImage_1.uploadImage.single('image'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.update));
router.put("/:id/variants", (0, authorize_1.authorize)('products.update'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.replaceVariants));
router.delete("/:id", (0, authorize_1.authorize)('products.delete'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.remove)); // ?hard=true
// Imagen binaria (para que el front la muestre)
router.get("/:id/image", (0, authorize_1.authorize)('products.read'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.getImage));
// Conversión SIMPLE → VARIANTED y VARIANTED → SIMPLE
router.post("/:id/convert-to-varianted", (0, authorize_1.authorize)('products.update'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.convertToVarianted));
router.post("/:id/convert-to-simple", (0, authorize_1.authorize)('products.update'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.convertToSimple));
//COMBOSSSSS
router.post("/combo", (0, authorize_1.authorize)('products.create'), uploadImage_1.uploadImage.single('image'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.createCombo));
router.post("/:id/combo-items", (0, authorize_1.authorize)('products.update'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.addComboItems));
router.patch("/combo-items/:comboItemId", (0, authorize_1.authorize)('products.update'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.updateComboItem));
router.delete("/combo-items/:comboItemId", (0, authorize_1.authorize)('products.update'), (0, asyncHandler_1.asyncHandler)(container_1.productsController.removeComboItem));
exports.default = router;
//# sourceMappingURL=products.routes.js.map