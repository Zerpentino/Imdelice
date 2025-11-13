"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const express_1 = require("express");
const authenticate_1 = require("../presentation/middlewares/authenticate");
const authorize_1 = require("../presentation/middlewares/authorize");
const asyncHandler_1 = require("../presentation/utils/asyncHandler");
const container_1 = require("../container");
const router = (0, express_1.Router)();
router.use(authenticate_1.authenticate);
router.get("/", (0, authorize_1.authorize)("orders.update"), (0, asyncHandler_1.asyncHandler)(container_1.channelConfigController.list));
router.put("/:source", (0, authorize_1.authorize)("orders.update"), (0, asyncHandler_1.asyncHandler)(container_1.channelConfigController.set));
exports.default = router;
//# sourceMappingURL=channelConfig.routes.js.map