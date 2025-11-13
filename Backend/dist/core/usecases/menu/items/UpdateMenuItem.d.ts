import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';
import type { MenuItem } from '@prisma/client';
type UpdateMenuItemInput = Partial<Pick<MenuItem, 'displayName' | 'displayPriceCents' | 'position' | 'isFeatured' | 'isActive'>>;
export declare class UpdateMenuItem {
    private readonly repo;
    constructor(repo: IMenuRepository);
    exec(id: number, data: UpdateMenuItemInput): Promise<{
        id: number;
        createdAt: Date;
        updatedAt: Date;
        position: number;
        isActive: boolean;
        sectionId: number;
        refType: import(".prisma/client").$Enums.MenuItemRefType;
        refId: number;
        displayName: string | null;
        displayPriceCents: number | null;
        isFeatured: boolean;
        deletedAt: Date | null;
    }>;
}
export {};
//# sourceMappingURL=UpdateMenuItem.d.ts.map