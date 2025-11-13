import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';
export declare class ListArchivedMenuSections {
    private readonly repo;
    constructor(repo: IMenuRepository);
    exec(menuId: number): Promise<({
        name: string;
        id: number;
        createdAt: Date;
        updatedAt: Date;
        position: number;
        isActive: boolean;
        categoryId: number | null;
        deletedAt: Date | null;
        menuId: number;
    } & {
        items: import(".prisma/client").MenuItem[];
    })[]>;
}
//# sourceMappingURL=ListArchivedMenuSections.d.ts.map