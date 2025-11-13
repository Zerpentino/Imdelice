import type { ICategoryRepository } from '../../domain/repositories/ICategoryRepository';
export declare class ListCategories {
    private repo;
    constructor(repo: ICategoryRepository);
    exec(): Promise<{
        name: string;
        id: number;
        slug: string;
        parentId: number | null;
        position: number;
        isActive: boolean;
        isComboOnly: boolean;
    }[]>;
}
//# sourceMappingURL=ListCategories.d.ts.map