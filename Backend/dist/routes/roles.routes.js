"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const express_1 = require("express");
const container_1 = require("../container");
const asyncHandler_1 = require("../presentation/utils/asyncHandler");
const authenticate_1 = require("../presentation/middlewares/authenticate");
const authorize_1 = require("../presentation/middlewares/authorize");
const r = (0, express_1.Router)();
// protege todas las rutas de /users
r.use(authenticate_1.authenticate);
r.get('/:id', (0, authorize_1.authorize)('roles.read'), (0, asyncHandler_1.asyncHandler)(container_1.rolesController.get));
r.get('/', (0, authorize_1.authorize)('roles.read'), (0, asyncHandler_1.asyncHandler)(container_1.rolesController.list));
r.post('/', (0, authorize_1.authorize)('roles.create'), (0, asyncHandler_1.asyncHandler)(container_1.rolesController.create));
r.put('/:id', (0, authorize_1.authorize)('roles.update'), (0, asyncHandler_1.asyncHandler)(container_1.rolesController.update));
r.delete('/:id', (0, authorize_1.authorize)('roles.delete'), (0, asyncHandler_1.asyncHandler)(container_1.rolesController.delete));
exports.default = r;
//# sourceMappingURL=roles.routes.js.map