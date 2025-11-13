import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export declare class ListProducts {
    private repo;
    constructor(repo: IProductRepository);
    exec(filter?: {
        categorySlug?: string;
        type?: 'SIMPLE' | 'VARIANTED' | 'COMBO';
        isActive?: boolean;
    }): Promise<import("../../domain/repositories/IProductRepository").ProductListItem[]>;
}
//# sourceMappingURL=ListProducts.d.ts.map