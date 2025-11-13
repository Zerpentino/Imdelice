"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const express_1 = require("express");
const asyncHandler_1 = require("../presentation/utils/asyncHandler");
const authenticate_1 = require("../presentation/middlewares/authenticate");
const authorize_1 = require("../presentation/middlewares/authorize");
const container_1 = require("../container");
const router = (0, express_1.Router)();
router.use(authenticate_1.authenticate);
// Menús
router.post("/", (0, authorize_1.authorize)('menu.create'), (0, asyncHandler_1.asyncHandler)(container_1.menuController.createMenu));
router.get("/", (0, authorize_1.authorize)('menu.read'), (0, asyncHandler_1.asyncHandler)(container_1.menuController.listMenus));
router.get("/trash", (0, authorize_1.authorize)('menu.read'), (0, asyncHandler_1.asyncHandler)(container_1.menuController.listArchivedMenus));
router.patch("/:id", (0, authorize_1.authorize)('menu.update'), (0, asyncHandler_1.asyncHandler)(container_1.menuController.updateMenu));
router.patch("/:id/restore", (0, authorize_1.authorize)('menu.update'), (0, asyncHandler_1.asyncHandler)(container_1.menuController.restoreMenu));
router.delete("/:id", (0, authorize_1.authorize)('menu.delete'), (0, asyncHandler_1.asyncHandler)(container_1.menuController.deleteMenu)); // ?hard=true
// Secciones
router.get("/:menuId/sections", (0, authorize_1.authorize)('menu.read'), (0, asyncHandler_1.asyncHandler)(container_1.menuController.listSections));
router.get("/:menuId/sections/trash", (0, authorize_1.authorize)('menu.read'), (0, asyncHandler_1.asyncHandler)(container_1.menuController.listArchivedSections));
router.post("/sections", (0, authorize_1.authorize)('menu.update'), (0, asyncHandler_1.asyncHandler)(container_1.menuController.createSection));
router.patch("/sections/:id", (0, authorize_1.authorize)('menu.update'), (0, asyncHandler_1.asyncHandler)(container_1.menuController.updateSection));
router.patch("/sections/:id/restore", (0, authorize_1.authorize)('menu.update'), (0, asyncHandler_1.asyncHandler)(container_1.menuController.restoreSection));
router.delete("/sections/:id", (0, authorize_1.authorize)('menu.delete'), (0, asyncHandler_1.asyncHandler)(container_1.menuController.deleteSection));
router.delete("/sections/:id/hard", (0, authorize_1.authorize)('menu.delete'), (0, asyncHandler_1.asyncHandler)(container_1.menuController.deleteSectionHard));
router.get("/sections/:sectionId/items", (0, authorize_1.authorize)('menu.read'), (0, asyncHandler_1.asyncHandler)(container_1.menuController.listItems));
router.get("/sections/:sectionId/items/trash", (0, authorize_1.authorize)('menu.read'), (0, asyncHandler_1.asyncHandler)(container_1.menuController.listArchivedItems));
// Items
router.post("/items", (0, authorize_1.authorize)('menu.update'), (0, asyncHandler_1.asyncHandler)(container_1.menuController.addItem));
router.patch("/items/:id", (0, authorize_1.authorize)('menu.update'), (0, asyncHandler_1.asyncHandler)(container_1.menuController.updateItem));
router.patch("/items/:id/restore", (0, authorize_1.authorize)('menu.update'), (0, asyncHandler_1.asyncHandler)(container_1.menuController.restoreItem));
router.delete("/items/:id", (0, authorize_1.authorize)('menu.delete'), (0, asyncHandler_1.asyncHandler)(container_1.menuController.removeItem));
router.delete("/items/:id/hard", (0, authorize_1.authorize)('menu.delete'), (0, asyncHandler_1.asyncHandler)(container_1.menuController.deleteItemHard));
// Consulta pública (reservada a personal con permiso de lectura)
router.get("/:id/public", (0, authorize_1.authorize)('menu.read'), (0, asyncHandler_1.asyncHandler)(container_1.menuController.getPublic));
exports.default = router;
//# sourceMappingURL=menu.routes.js.map