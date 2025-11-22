"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const express_1 = require("express");
const authenticate_1 = require("../presentation/middlewares/authenticate");
const authorize_1 = require("../presentation/middlewares/authorize");
const asyncHandler_1 = require("../presentation/utils/asyncHandler");
const container_1 = require("../container");
const router = (0, express_1.Router)();
router.use(authenticate_1.authenticate);
router.get("/", (0, authorize_1.authorize)("expenses.read"), (0, asyncHandler_1.asyncHandler)(container_1.expensesController.list));
router.get("/summary", (0, authorize_1.authorize)("expenses.read"), (0, asyncHandler_1.asyncHandler)(container_1.expensesController.summary));
router.get("/:id", (0, authorize_1.authorize)("expenses.read"), (0, asyncHandler_1.asyncHandler)(container_1.expensesController.get));
router.post("/", (0, authorize_1.authorize)("expenses.manage"), (0, asyncHandler_1.asyncHandler)(container_1.expensesController.create));
router.patch("/:id", (0, authorize_1.authorize)("expenses.manage"), (0, asyncHandler_1.asyncHandler)(container_1.expensesController.update));
router.delete("/:id", (0, authorize_1.authorize)("expenses.manage"), (0, asyncHandler_1.asyncHandler)(container_1.expensesController.remove));
exports.default = router;
//# sourceMappingURL=expenses.routes.js.map