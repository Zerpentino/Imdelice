import { Router } from "express";
import { asyncHandler } from "../presentation/utils/asyncHandler";
import { authenticate } from "../presentation/middlewares/authenticate";
import { authorize } from "../presentation/middlewares/authorize";
import { menuController } from "../container";

const router = Router();

router.use(authenticate);

// Menús
router.post("/", authorize('menu.create'), asyncHandler(menuController.createMenu));
router.get("/",  authorize('menu.read'), asyncHandler(menuController.listMenus));
router.get("/trash", authorize('menu.read'), asyncHandler(menuController.listArchivedMenus));
router.patch("/:id", authorize('menu.update'), asyncHandler(menuController.updateMenu));
router.patch("/:id/restore", authorize('menu.update'), asyncHandler(menuController.restoreMenu));
router.delete("/:id", authorize('menu.delete'), asyncHandler(menuController.deleteMenu)); // ?hard=true

// Secciones
router.get("/:menuId/sections", authorize('menu.read'), asyncHandler(menuController.listSections));
router.get("/:menuId/sections/trash", authorize('menu.read'), asyncHandler(menuController.listArchivedSections));
router.post("/sections", authorize('menu.update'), asyncHandler(menuController.createSection));
router.patch("/sections/:id", authorize('menu.update'), asyncHandler(menuController.updateSection));
router.patch("/sections/:id/restore", authorize('menu.update'), asyncHandler(menuController.restoreSection));
router.delete("/sections/:id", authorize('menu.delete'), asyncHandler(menuController.deleteSection));
router.delete("/sections/:id/hard", authorize('menu.delete'), asyncHandler(menuController.deleteSectionHard));

router.get("/sections/:sectionId/items", authorize('menu.read'), asyncHandler(menuController.listItems));
router.get("/sections/:sectionId/items/trash", authorize('menu.read'), asyncHandler(menuController.listArchivedItems));

// Items
router.post("/items", authorize('menu.update'), asyncHandler(menuController.addItem));
router.patch("/items/:id", authorize('menu.update'), asyncHandler(menuController.updateItem));
router.patch("/items/:id/restore", authorize('menu.update'), asyncHandler(menuController.restoreItem));
router.delete("/items/:id", authorize('menu.delete'), asyncHandler(menuController.removeItem));
router.delete("/items/:id/hard", authorize('menu.delete'), asyncHandler(menuController.deleteItemHard));

// Consulta pública (reservada a personal con permiso de lectura)
router.get("/:id/public", authorize('menu.read'), asyncHandler(menuController.getPublic));

export default router;
