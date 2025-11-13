"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const express_1 = require("express");
const asyncHandler_1 = require("../presentation/utils/asyncHandler");
const authenticate_1 = require("../presentation/middlewares/authenticate");
const container_1 = require("../container");
const authorize_1 = require("../presentation/middlewares/authorize");
const router = (0, express_1.Router)();
router.use(authenticate_1.authenticate);
router.post("/groups", (0, authorize_1.authorize)('modifiers.create'), (0, asyncHandler_1.asyncHandler)(container_1.modifiersController.createGroup));
router.patch("/groups/:id", (0, authorize_1.authorize)('modifiers.update'), (0, asyncHandler_1.asyncHandler)(container_1.modifiersController.updateGroup));
router.delete("/groups/:id", (0, authorize_1.authorize)('modifiers.delete'), (0, asyncHandler_1.asyncHandler)(container_1.modifiersController.removeGroup)); // ?hard=true
router.patch('/modifier-options/:optionId', (0, authorize_1.authorize)('modifiers.update'), container_1.modifiersController.updateOption);
// LISTADOS
router.get("/groups", (0, authorize_1.authorize)('modifiers.read'), (0, asyncHandler_1.asyncHandler)(container_1.modifiersController.listGroups));
router.get("/groups/by-product/:productId", (0, authorize_1.authorize)('modifiers.read'), (0, asyncHandler_1.asyncHandler)(container_1.modifiersController.listByProduct)); // poner antes de :id
router.get("/groups/:id", (0, authorize_1.authorize)('modifiers.read'), (0, asyncHandler_1.asyncHandler)(container_1.modifiersController.getGroup));
router.get("/groups/:groupId/products", (0, authorize_1.authorize)('modifiers.read'), (0, asyncHandler_1.asyncHandler)(container_1.modifiersController.listProductsByGroup));
exports.default = router;
//# sourceMappingURL=modifiers.routes.js.map