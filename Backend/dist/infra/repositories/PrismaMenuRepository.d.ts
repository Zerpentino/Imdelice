import type { IMenuRepository, MenuItemWithRef, MenuPublic } from '../../core/domain/repositories/IMenuRepository';
import type { Menu, MenuSection, MenuItem, MenuItemRefType } from '@prisma/client';
export declare class PrismaMenuRepository implements IMenuRepository {
    createMenu(data: {
        name: string;
        isActive?: boolean;
        publishedAt?: Date | null;
        version?: number;
    }): Promise<Menu>;
    listMenus(): Promise<Menu[]>;
    listArchivedMenus(): Promise<Menu[]>;
    updateMenu(id: number, data: Partial<Pick<Menu, 'name' | 'isActive' | 'publishedAt' | 'version'>>): Promise<Menu>;
    deleteMenu(id: number, hard?: boolean): Promise<void>;
    restoreMenu(id: number): Promise<void>;
    createSection(data: {
        menuId: number;
        name: string;
        position?: number;
        isActive?: boolean;
        categoryId?: number | null;
    }): Promise<MenuSection>;
    updateSection(id: number, data: Partial<Pick<MenuSection, 'name' | 'position' | 'isActive' | 'categoryId'>>): Promise<MenuSection>;
    deleteSection(id: number, hard?: boolean): Promise<void>;
    restoreSection(id: number): Promise<void>;
    deleteSectionHard(id: number): Promise<void>;
    listSections(menuId: number): Promise<(MenuSection & {
        items: MenuItem[];
    })[]>;
    listArchivedSections(menuId: number): Promise<(MenuSection & {
        items: MenuItem[];
    })[]>;
    addItem(data: {
        sectionId: number;
        refType: MenuItemRefType;
        refId: number;
        displayName?: string | null;
        displayPriceCents?: number | null;
        position?: number;
        isFeatured?: boolean;
        isActive?: boolean;
    }): Promise<MenuItem>;
    updateItem(id: number, data: Partial<Pick<MenuItem, 'displayName' | 'displayPriceCents' | 'position' | 'isFeatured' | 'isActive'>>): Promise<MenuItem>;
    removeItem(id: number, hard?: boolean): Promise<void>;
    restoreItem(id: number): Promise<void>;
    deleteItemHard(id: number): Promise<void>;
    listItems(sectionId: number): Promise<MenuItemWithRef[]>;
    listArchivedItems(sectionId: number): Promise<MenuItemWithRef[]>;
    getMenuPublic(id: number): Promise<MenuPublic | null>;
    private ensureMenuActive;
    private ensureSectionActive;
    private ensureItemActive;
    private ensureCategoryExists;
    private assertProductCanBeListed;
    private ensureRefExists;
    private resolveMenuItemRefs;
}
//# sourceMappingURL=PrismaMenuRepository.d.ts.map