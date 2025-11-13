import { Request, Response } from 'express';
import { success, fail } from '../utils/apiResponse';
import { CreateMenu } from '../../core/usecases/menu/CreateMenu';
import { ListMenus } from '../../core/usecases/menu/ListMenus';
import { ListArchivedMenus } from '../../core/usecases/menu/ListArchivedMenus';
import { UpdateMenu } from '../../core/usecases/menu/UpdateMenu';
import { DeleteMenu } from '../../core/usecases/menu/DeleteMenu';
import { RestoreMenu } from '../../core/usecases/menu/RestoreMenu';
import { CreateMenuSection } from '../../core/usecases/menu/sections/CreateMenuSection';
import { UpdateMenuSection } from '../../core/usecases/menu/sections/UpdateMenuSection';
import { DeleteMenuSection } from '../../core/usecases/menu/sections/DeleteMenuSection';
import { DeleteMenuSectionHard } from '../../core/usecases/menu/sections/DeleteMenuSectionHard';
import { ListMenuSections } from '../../core/usecases/menu/sections/ListMenuSections';
import { ListArchivedMenuSections } from '../../core/usecases/menu/sections/ListArchivedMenuSections';
import { RestoreMenuSection } from '../../core/usecases/menu/sections/RestoreMenuSection';
import { AddMenuItem } from '../../core/usecases/menu/items/AddMenuItem';
import { UpdateMenuItem } from '../../core/usecases/menu/items/UpdateMenuItem';
import { RemoveMenuItem } from '../../core/usecases/menu/items/RemoveMenuItem';
import { RestoreMenuItem } from '../../core/usecases/menu/items/RestoreMenuItem';
import { DeleteMenuItemHard } from '../../core/usecases/menu/items/DeleteMenuItemHard';
import { ListMenuItems } from '../../core/usecases/menu/items/ListMenuItems';
import { ListArchivedMenuItems } from '../../core/usecases/menu/items/ListArchivedMenuItems';
import { GetMenuPublic } from '../../core/usecases/menu/items/GetMenuPublic';
import {
  CreateMenuDto,
  UpdateMenuDto,
  CreateMenuSectionDto,
  UpdateMenuSectionDto,
  AddMenuItemDto,
  UpdateMenuItemDto
} from '../dtos/menu.dto';

export class MenuController {
  constructor(
    private createMenuUC: CreateMenu,
    private listMenusUC: ListMenus,
    private listArchivedMenusUC: ListArchivedMenus,
    private updateMenuUC: UpdateMenu,
    private deleteMenuUC: DeleteMenu,
    private restoreMenuUC: RestoreMenu,
    private createSectionUC: CreateMenuSection,
    private updateSectionUC: UpdateMenuSection,
    private deleteSectionUC: DeleteMenuSection,
    private deleteSectionHardUC: DeleteMenuSectionHard,
    private restoreSectionUC: RestoreMenuSection,
    private listSectionsUC: ListMenuSections,
    private listArchivedSectionsUC: ListArchivedMenuSections,
    private addItemUC: AddMenuItem,
    private updateItemUC: UpdateMenuItem,
    private removeItemUC: RemoveMenuItem,
    private restoreItemUC: RestoreMenuItem,
    private deleteItemHardUC: DeleteMenuItemHard,
    private listItemsUC: ListMenuItems,
    private listArchivedItemsUC: ListArchivedMenuItems,
    private getMenuPublicUC: GetMenuPublic
  ) {}

  createMenu = async (req: Request, res: Response) => {
    try {
      const dto = CreateMenuDto.parse(req.body);
      const created = await this.createMenuUC.exec(dto);
      return success(res, created, 'Created', 201);
    } catch (e: any) {
      return fail(res, e?.message || 'Error creating menu', 400, e);
    }
  };

  listMenus = async (_req: Request, res: Response) => {
    try {
      const menus = await this.listMenusUC.exec();
      return success(res, menus);
    } catch (e: any) {
      return fail(res, e?.message || 'Error listing menus', 400, e);
    }
  };

  listArchivedMenus = async (_req: Request, res: Response) => {
    try {
      const menus = await this.listArchivedMenusUC.exec();
      return success(res, menus);
    } catch (e: any) {
      return fail(res, e?.message || 'Error listing archived menus', 400, e);
    }
  };

  updateMenu = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      const dto = UpdateMenuDto.parse(req.body);
      const updated = await this.updateMenuUC.exec(id, dto);
      return success(res, updated, 'Updated');
    } catch (e: any) {
      return fail(res, e?.message || 'Error updating menu', 400, e);
    }
  };

  deleteMenu = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      const hard = req.query.hard === 'true';
      await this.deleteMenuUC.exec(id, hard);
      return success(res, null, hard ? 'Deleted' : 'Archived', 204);
    } catch (e: any) {
      return fail(res, e?.message || 'Error deleting menu', 400, e);
    }
  };

  restoreMenu = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      await this.restoreMenuUC.exec(id);
      return success(res, null, 'Restored', 204);
    } catch (e: any) {
      return fail(res, e?.message || 'Error restoring menu', 400, e);
    }
  };

  createSection = async (req: Request, res: Response) => {
    try {
      const dto = CreateMenuSectionDto.parse(req.body);
      const created = await this.createSectionUC.exec(dto);
      return success(res, created, 'Created', 201);
    } catch (e: any) {
      return fail(res, e?.message || 'Error creating section', 400, e);
    }
  };

  listSections = async (req: Request, res: Response) => {
    try {
      const menuId = Number(req.params.menuId);
      const sections = await this.listSectionsUC.exec(menuId);
      return success(res, sections);
    } catch (e: any) {
      return fail(res, e?.message || 'Error listing sections', 400, e);
    }
  };

  listArchivedSections = async (req: Request, res: Response) => {
    try {
      const menuId = Number(req.params.menuId);
      const sections = await this.listArchivedSectionsUC.exec(menuId);
      return success(res, sections);
    } catch (e: any) {
      return fail(res, e?.message || 'Error listing archived sections', 400, e);
    }
  };

  updateSection = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      const dto = UpdateMenuSectionDto.parse(req.body);
      const updated = await this.updateSectionUC.exec(id, dto);
      return success(res, updated, 'Updated');
    } catch (e: any) {
      return fail(res, e?.message || 'Error updating section', 400, e);
    }
  };

  deleteSection = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      await this.deleteSectionUC.exec(id, false);
      return success(res, null, 'Archived', 204);
    } catch (e: any) {
      return fail(res, e?.message || 'Error deleting section', 400, e);
    }
  };

  deleteSectionHard = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      await this.deleteSectionHardUC.exec(id);
      return success(res, null, 'Deleted', 204);
    } catch (e: any) {
      return fail(res, e?.message || 'Error deleting section', 400, e);
    }
  };

  restoreSection = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      await this.restoreSectionUC.exec(id);
      return success(res, null, 'Restored', 204);
    } catch (e: any) {
      return fail(res, e?.message || 'Error restoring section', 400, e);
    }
  };

  addItem = async (req: Request, res: Response) => {
    try {
      const dto = AddMenuItemDto.parse(req.body);
      const created = await this.addItemUC.exec(dto);
      return success(res, created, 'Created', 201);
    } catch (e: any) {
      return fail(res, e?.message || 'Error adding item', 400, e);
    }
  };

  updateItem = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      const dto = UpdateMenuItemDto.parse(req.body);
      const updated = await this.updateItemUC.exec(id, dto);
      return success(res, updated, 'Updated');
    } catch (e: any) {
      return fail(res, e?.message || 'Error updating item', 400, e);
    }
  };

  removeItem = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      await this.removeItemUC.exec(id, false);
      return success(res, null, 'Archived', 204);
    } catch (e: any) {
      return fail(res, e?.message || 'Error deleting item', 400, e);
    }
  };

  deleteItemHard = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      await this.deleteItemHardUC.exec(id);
      return success(res, null, 'Deleted', 204);
    } catch (e: any) {
      return fail(res, e?.message || 'Error deleting item', 400, e);
    }
  };

  restoreItem = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      await this.restoreItemUC.exec(id);
      return success(res, null, 'Restored', 204);
    } catch (e: any) {
      return fail(res, e?.message || 'Error restoring item', 400, e);
    }
  };
  getPublic = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      const data = await this.getMenuPublicUC.exec(id);
      if (!data) return fail(res, 'Not found', 404);
      return success(res, data);
    } catch (e: any) {
      return fail(res, e?.message || 'Error fetching menu', 400, e);
    }
  };

  listItems = async (req: Request, res: Response) => {
    try {
      const sectionId = Number(req.params.sectionId);
      const items = await this.listItemsUC.exec(sectionId);
      return success(res, items);
    } catch (e: any) {
      return fail(res, e?.message || 'Error listing items', 400, e);
    }
  };

  listArchivedItems = async (req: Request, res: Response) => {
    try {
      const sectionId = Number(req.params.sectionId);
      const items = await this.listArchivedItemsUC.exec(sectionId);
      return success(res, items);
    } catch (e: any) {
      return fail(res, e?.message || 'Error listing archived items', 400, e);
    }
  };
}
