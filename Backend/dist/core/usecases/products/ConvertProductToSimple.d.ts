import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export declare class ConvertProductToSimple {
    private repo;
    constructor(repo: IProductRepository);
    exec(productId: number, priceCents: number): Promise<void>;
}
//# sourceMappingURL=ConvertProductToSimple.d.ts.map