"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const express_1 = require("express");
const container_1 = require("../container");
const asyncHandler_1 = require("../presentation/utils/asyncHandler");
const authenticate_1 = require("../presentation/middlewares/authenticate");
const authorize_1 = require("../presentation/middlewares/authorize");
const router = (0, express_1.Router)();
// protege todas las rutas de /users
router.use(authenticate_1.authenticate);
router.get('/', (0, authorize_1.authorize)('users.read'), (0, asyncHandler_1.asyncHandler)(container_1.usersController.list));
router.post('/', (0, authorize_1.authorize)('users.create'), (0, asyncHandler_1.asyncHandler)(container_1.usersController.create));
router.put('/:id', (0, authorize_1.authorize)('users.update'), (0, asyncHandler_1.asyncHandler)(container_1.usersController.update));
router.delete('/:id', (0, authorize_1.authorize)('users.delete'), (0, asyncHandler_1.asyncHandler)(container_1.usersController.delete));
exports.default = router;
//# sourceMappingURL=users.routes.js.map