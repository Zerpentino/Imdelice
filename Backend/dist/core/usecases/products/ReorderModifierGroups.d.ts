import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export declare class ReorderModifierGroups {
    private repo;
    constructor(repo: IProductRepository);
    exec(productId: number, items: Array<{
        linkId: number;
        position: number;
    }>): Promise<void>;
}
//# sourceMappingURL=ReorderModifierGroups.d.ts.map