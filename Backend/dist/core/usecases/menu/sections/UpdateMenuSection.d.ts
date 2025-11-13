import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';
import type { MenuSection } from '@prisma/client';
type UpdateSectionInput = Partial<Pick<MenuSection, 'name' | 'position' | 'isActive' | 'categoryId'>>;
export declare class UpdateMenuSection {
    private readonly repo;
    constructor(repo: IMenuRepository);
    exec(id: number, data: UpdateSectionInput): Promise<{
        name: string;
        id: number;
        createdAt: Date;
        updatedAt: Date;
        position: number;
        isActive: boolean;
        categoryId: number | null;
        deletedAt: Date | null;
        menuId: number;
    }>;
}
export {};
//# sourceMappingURL=UpdateMenuSection.d.ts.map