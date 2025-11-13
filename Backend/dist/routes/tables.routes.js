"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const express_1 = require("express");
const asyncHandler_1 = require("../presentation/utils/asyncHandler");
const authenticate_1 = require("../presentation/middlewares/authenticate");
const authorize_1 = require("../presentation/middlewares/authorize");
const container_1 = require("../container");
const router = (0, express_1.Router)();
router.use(authenticate_1.authenticate);
router.post('/', (0, authorize_1.authorize)('tables.create'), (0, asyncHandler_1.asyncHandler)(container_1.tablesController.create));
router.get('/', (0, authorize_1.authorize)('tables.read'), (0, asyncHandler_1.asyncHandler)(container_1.tablesController.list));
router.get('/:id', (0, authorize_1.authorize)('tables.read'), (0, asyncHandler_1.asyncHandler)(container_1.tablesController.getOne));
router.patch('/:id', (0, authorize_1.authorize)('tables.update'), (0, asyncHandler_1.asyncHandler)(container_1.tablesController.update));
router.delete('/:id', (0, authorize_1.authorize)('tables.delete'), (0, asyncHandler_1.asyncHandler)(container_1.tablesController.remove));
exports.default = router;
//# sourceMappingURL=tables.routes.js.map