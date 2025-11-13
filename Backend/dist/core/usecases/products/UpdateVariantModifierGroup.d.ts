import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export declare class UpdateVariantModifierGroup {
    private readonly repo;
    constructor(repo: IProductRepository);
    exec(variantId: number, groupId: number, data: {
        minSelect?: number;
        maxSelect?: number | null;
        isRequired?: boolean;
    }): Promise<void>;
}
//# sourceMappingURL=UpdateVariantModifierGroup.d.ts.map