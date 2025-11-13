import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';
export declare class ListMenuSections {
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
//# sourceMappingURL=ListMenuSections.d.ts.map