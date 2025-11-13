import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export declare class ConvertProductToVarianted {
    private repo;
    constructor(repo: IProductRepository);
    exec(productId: number, variants: {
        name: string;
        priceCents: number;
        sku?: string;
    }[]): Promise<void>;
}
//# sourceMappingURL=ConvertProductToVarianted.d.ts.map