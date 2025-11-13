import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export declare class GetProductDetail {
    private repo;
    constructor(repo: IProductRepository);
    exec(id: number): Promise<import("../../domain/repositories/IProductRepository").ProductWithDetails | null>;
}
//# sourceMappingURL=GetProductDetail.d.ts.map