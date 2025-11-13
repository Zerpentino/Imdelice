import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';
import type { MenuItem } from '@prisma/client';
type AddMenuItemInput = {
    sectionId: number;
    refType: MenuItem['refType'];
    refId: number;
    displayName?: string | null;
    displayPriceCents?: number | null;
    position?: number;
    isFeatured?: boolean;
    isActive?: boolean;
};
export declare class AddMenuItem {
    private readonly repo;
    constructor(repo: IMenuRepository);
    exec(input: AddMenuItemInput): Promise<{
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
//# sourceMappingURL=AddMenuItem.d.ts.map