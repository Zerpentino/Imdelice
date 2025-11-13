"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const express_1 = require("express");
const asyncHandler_1 = require("../presentation/utils/asyncHandler");
const authenticate_1 = require("../presentation/middlewares/authenticate");
const container_1 = require("../container");
const authorize_1 = require("../presentation/middlewares/authorize");
const router = (0, express_1.Router)();
router.use(authenticate_1.authenticate);
router.post('/', (0, authorize_1.authorize)('categories.create'), (0, asyncHandler_1.asyncHandler)(container_1.categoriesController.create));
router.get('/', (0, authorize_1.authorize)('categories.read'), (0, asyncHandler_1.asyncHandler)(container_1.categoriesController.list));
router.patch('/:id', (0, authorize_1.authorize)('categories.update'), (0, asyncHandler_1.asyncHandler)(container_1.categoriesController.update));
router.delete('/:id', (0, authorize_1.authorize)('categories.delete'), (0, asyncHandler_1.asyncHandler)(container_1.categoriesController.remove));
exports.default = router;
//# sourceMappingURL=categories.routes.js.map