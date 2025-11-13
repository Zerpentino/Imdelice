import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export declare class DetachModifierGroupFromVariant {
    private readonly repo;
    constructor(repo: IProductRepository);
    exec(variantId: number, groupId: number): Promise<void>;
}
//# sourceMappingURL=DetachModifierGroupFromVariant.d.ts.map