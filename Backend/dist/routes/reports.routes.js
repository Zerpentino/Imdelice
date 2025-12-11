"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const express_1 = require("express");
const asyncHandler_1 = require("../presentation/utils/asyncHandler");
const authenticate_1 = require("../presentation/middlewares/authenticate");
const authorize_1 = require("../presentation/middlewares/authorize");
const container_1 = require("../container");
const router = (0, express_1.Router)();
router.use(authenticate_1.authenticate);
router.get("/payments", (0, authorize_1.authorize)("expenses.read"), (0, asyncHandler_1.asyncHandler)(container_1.reportsController.payments));
router.get("/profit-loss", (0, authorize_1.authorize)("expenses.read"), (0, asyncHandler_1.asyncHandler)(container_1.reportsController.profitLoss));
exports.default = router;
//# sourceMappingURL=reports.routes.js.map