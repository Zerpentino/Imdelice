import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export declare class DetachModifierGroupFromProduct {
    private repo;
    constructor(repo: IProductRepository);
    exec(linkIdOr: {
        linkId: number;
    } | {
        productId: number;
        groupId: number;
    }): Promise<void>;
}
//# sourceMappingURL=DetachModifierGroupFromProduct.d.ts.map