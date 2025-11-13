import type { ICategoryRepository } from '../../domain/repositories/ICategoryRepository';
export declare class UpdateCategory {
    private repo;
    constructor(repo: ICategoryRepository);
    exec(id: number, data: any): Promise<{
        name: string;
        id: number;
        slug: string;
        parentId: number | null;
        position: number;
        isActive: boolean;
        isComboOnly: boolean;
    }>;
}
//# sourceMappingURL=UpdateCategory.d.ts.map