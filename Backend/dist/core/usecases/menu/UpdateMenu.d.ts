import type { IMenuRepository } from '../../domain/repositories/IMenuRepository';
import type { Menu } from '@prisma/client';
type UpdateMenuInput = Partial<Pick<Menu, 'name' | 'isActive' | 'publishedAt' | 'version'>>;
export declare class UpdateMenu {
    private readonly repo;
    constructor(repo: IMenuRepository);
    exec(id: number, data: UpdateMenuInput): Promise<{
        name: string;
        id: number;
        createdAt: Date;
        updatedAt: Date;
        isActive: boolean;
        deletedAt: Date | null;
        publishedAt: Date | null;
        version: number;
    }>;
}
export {};
//# sourceMappingURL=UpdateMenu.d.ts.map