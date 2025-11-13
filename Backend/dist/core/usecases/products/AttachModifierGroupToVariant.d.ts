import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export declare class AttachModifierGroupToVariant {
    private readonly repo;
    constructor(repo: IProductRepository);
    exec(input: {
        variantId: number;
        groupId: number;
        minSelect?: number;
        maxSelect?: number | null;
        isRequired?: boolean;
    }): Promise<void>;
}
//# sourceMappingURL=AttachModifierGroupToVariant.d.ts.map