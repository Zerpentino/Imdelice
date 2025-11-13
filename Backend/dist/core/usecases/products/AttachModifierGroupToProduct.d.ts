import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export declare class AttachModifierGroupToProduct {
    private repo;
    constructor(repo: IProductRepository);
    exec(input: {
        productId: number;
        groupId: number;
        position?: number;
    }): Promise<void>;
}
//# sourceMappingURL=AttachModifierGroupToProduct.d.ts.map