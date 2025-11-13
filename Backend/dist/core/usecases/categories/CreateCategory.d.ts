import type { ICategoryRepository } from '../../domain/repositories/ICategoryRepository';
export declare class CreateCategory {
    private repo;
    constructor(repo: ICategoryRepository);
    exec(input: {
        name: string;
        slug: string;
        parentId?: number | null;
        position?: number;
        isComboOnly?: boolean;
    }): Promise<{
        name: string;
        id: number;
        slug: string;
        parentId: number | null;
        position: number;
        isActive: boolean;
        isComboOnly: boolean;
    }>;
}
//# sourceMappingURL=CreateCategory.d.ts.map