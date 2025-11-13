import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export declare class DeleteProduct {
    private repo;
    constructor(repo: IProductRepository);
    exec(id: number, hard?: boolean): Promise<void>;
}
//# sourceMappingURL=DeleteProduct.d.ts.map