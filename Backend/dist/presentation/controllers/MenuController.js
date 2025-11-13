"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.MenuController = void 0;
const apiResponse_1 = require("../utils/apiResponse");
const menu_dto_1 = require("../dtos/menu.dto");
class MenuController {
    constructor(createMenuUC, listMenusUC, listArchivedMenusUC, updateMenuUC, deleteMenuUC, restoreMenuUC, createSectionUC, updateSectionUC, deleteSectionUC, deleteSectionHardUC, restoreSectionUC, listSectionsUC, listArchivedSectionsUC, addItemUC, updateItemUC, removeItemUC, restoreItemUC, deleteItemHardUC, listItemsUC, listArchivedItemsUC, getMenuPublicUC) {
        this.createMenuUC = createMenuUC;
        this.listMenusUC = listMenusUC;
        this.listArchivedMenusUC = listArchivedMenusUC;
        this.updateMenuUC = updateMenuUC;
        this.deleteMenuUC = deleteMenuUC;
        this.restoreMenuUC = restoreMenuUC;
        this.createSectionUC = createSectionUC;
        this.updateSectionUC = updateSectionUC;
        this.deleteSectionUC = deleteSectionUC;
        this.deleteSectionHardUC = deleteSectionHardUC;
        this.restoreSectionUC = restoreSectionUC;
        this.listSectionsUC = listSectionsUC;
        this.listArchivedSectionsUC = listArchivedSectionsUC;
        this.addItemUC = addItemUC;
        this.updateItemUC = updateItemUC;
        this.removeItemUC = removeItemUC;
        this.restoreItemUC = restoreItemUC;
        this.deleteItemHardUC = deleteItemHardUC;
        this.listItemsUC = listItemsUC;
        this.listArchivedItemsUC = listArchivedItemsUC;
        this.getMenuPublicUC = getMenuPublicUC;
        this.createMenu = async (req, res) => {
            try {
                const dto = menu_dto_1.CreateMenuDto.parse(req.body);
                const created = await this.createMenuUC.exec(dto);
                return (0, apiResponse_1.success)(res, created, 'Created', 201);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || 'Error creating menu', 400, e);
            }
        };
        this.listMenus = async (_req, res) => {
            try {
                const menus = await this.listMenusUC.exec();
                return (0, apiResponse_1.success)(res, menus);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || 'Error listing menus', 400, e);
            }
        };
        this.listArchivedMenus = async (_req, res) => {
            try {
                const menus = await this.listArchivedMenusUC.exec();
                return (0, apiResponse_1.success)(res, menus);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || 'Error listing archived menus', 400, e);
            }
        };
        this.updateMenu = async (req, res) => {
            try {
                const id = Number(req.params.id);
                const dto = menu_dto_1.UpdateMenuDto.parse(req.body);
                const updated = await this.updateMenuUC.exec(id, dto);
                return (0, apiResponse_1.success)(res, updated, 'Updated');
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || 'Error updating menu', 400, e);
            }
        };
        this.deleteMenu = async (req, res) => {
            try {
                const id = Number(req.params.id);
                const hard = req.query.hard === 'true';
                await this.deleteMenuUC.exec(id, hard);
                return (0, apiResponse_1.success)(res, null, hard ? 'Deleted' : 'Archived', 204);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || 'Error deleting menu', 400, e);
            }
        };
        this.restoreMenu = async (req, res) => {
            try {
                const id = Number(req.params.id);
                await this.restoreMenuUC.exec(id);
                return (0, apiResponse_1.success)(res, null, 'Restored', 204);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || 'Error restoring menu', 400, e);
            }
        };
        this.createSection = async (req, res) => {
            try {
                const dto = menu_dto_1.CreateMenuSectionDto.parse(req.body);
                const created = await this.createSectionUC.exec(dto);
                return (0, apiResponse_1.success)(res, created, 'Created', 201);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || 'Error creating section', 400, e);
            }
        };
        this.listSections = async (req, res) => {
            try {
                const menuId = Number(req.params.menuId);
                const sections = await this.listSectionsUC.exec(menuId);
                return (0, apiResponse_1.success)(res, sections);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || 'Error listing sections', 400, e);
            }
        };
        this.listArchivedSections = async (req, res) => {
            try {
                const menuId = Number(req.params.menuId);
                const sections = await this.listArchivedSectionsUC.exec(menuId);
                return (0, apiResponse_1.success)(res, sections);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || 'Error listing archived sections', 400, e);
            }
        };
        this.updateSection = async (req, res) => {
            try {
                const id = Number(req.params.id);
                const dto = menu_dto_1.UpdateMenuSectionDto.parse(req.body);
                const updated = await this.updateSectionUC.exec(id, dto);
                return (0, apiResponse_1.success)(res, updated, 'Updated');
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || 'Error updating section', 400, e);
            }
        };
        this.deleteSection = async (req, res) => {
            try {
                const id = Number(req.params.id);
                await this.deleteSectionUC.exec(id, false);
                return (0, apiResponse_1.success)(res, null, 'Archived', 204);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || 'Error deleting section', 400, e);
            }
        };
        this.deleteSectionHard = async (req, res) => {
            try {
                const id = Number(req.params.id);
                await this.deleteSectionHardUC.exec(id);
                return (0, apiResponse_1.success)(res, null, 'Deleted', 204);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || 'Error deleting section', 400, e);
            }
        };
        this.restoreSection = async (req, res) => {
            try {
                const id = Number(req.params.id);
                await this.restoreSectionUC.exec(id);
                return (0, apiResponse_1.success)(res, null, 'Restored', 204);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || 'Error restoring section', 400, e);
            }
        };
        this.addItem = async (req, res) => {
            try {
                const dto = menu_dto_1.AddMenuItemDto.parse(req.body);
                const created = await this.addItemUC.exec(dto);
                return (0, apiResponse_1.success)(res, created, 'Created', 201);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || 'Error adding item', 400, e);
            }
        };
        this.updateItem = async (req, res) => {
            try {
                const id = Number(req.params.id);
                const dto = menu_dto_1.UpdateMenuItemDto.parse(req.body);
                const updated = await this.updateItemUC.exec(id, dto);
                return (0, apiResponse_1.success)(res, updated, 'Updated');
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || 'Error updating item', 400, e);
            }
        };
        this.removeItem = async (req, res) => {
            try {
                const id = Number(req.params.id);
                await this.removeItemUC.exec(id, false);
                return (0, apiResponse_1.success)(res, null, 'Archived', 204);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || 'Error deleting item', 400, e);
            }
        };
        this.deleteItemHard = async (req, res) => {
            try {
                const id = Number(req.params.id);
                await this.deleteItemHardUC.exec(id);
                return (0, apiResponse_1.success)(res, null, 'Deleted', 204);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || 'Error deleting item', 400, e);
            }
        };
        this.restoreItem = async (req, res) => {
            try {
                const id = Number(req.params.id);
                await this.restoreItemUC.exec(id);
                return (0, apiResponse_1.success)(res, null, 'Restored', 204);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || 'Error restoring item', 400, e);
            }
        };
        this.getPublic = async (req, res) => {
            try {
                const id = Number(req.params.id);
                const data = await this.getMenuPublicUC.exec(id);
                if (!data)
                    return (0, apiResponse_1.fail)(res, 'Not found', 404);
                return (0, apiResponse_1.success)(res, data);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || 'Error fetching menu', 400, e);
            }
        };
        this.listItems = async (req, res) => {
            try {
                const sectionId = Number(req.params.sectionId);
                const items = await this.listItemsUC.exec(sectionId);
                return (0, apiResponse_1.success)(res, items);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || 'Error listing items', 400, e);
            }
        };
        this.listArchivedItems = async (req, res) => {
            try {
                const sectionId = Number(req.params.sectionId);
                const items = await this.listArchivedItemsUC.exec(sectionId);
                return (0, apiResponse_1.success)(res, items);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || 'Error listing archived items', 400, e);
            }
        };
    }
}
exports.MenuController = MenuController;
//# sourceMappingURL=MenuController.js.map