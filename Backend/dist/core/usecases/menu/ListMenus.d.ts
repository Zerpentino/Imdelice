import type { IMenuRepository } from '../../domain/repositories/IMenuRepository';
export declare class ListMenus {
    private readonly repo;
    constructor(repo: IMenuRepository);
    exec(): Promise<{
        name: string;
        id: number;
        createdAt: Date;
        updatedAt: Date;
        isActive: boolean;
        deletedAt: Date | null;
        publishedAt: Date | null;
        version: number;
    }[]>;
}
//# sourceMappingURL=ListMenus.d.ts.map