import type { ICategoryRepository } from '../../domain/repositories/ICategoryRepository';
export declare class DeleteCategory {
    private repo;
    constructor(repo: ICategoryRepository);
    exec(id: number, hard?: boolean): Promise<void>;
}
//# sourceMappingURL=DeleteCategory.d.ts.map