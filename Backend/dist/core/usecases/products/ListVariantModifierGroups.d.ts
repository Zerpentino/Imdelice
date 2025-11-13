import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export declare class ListVariantModifierGroups {
    private readonly repo;
    constructor(repo: IProductRepository);
    exec(variantId: number): Promise<{
        group: import(".prisma/client").ModifierGroup & {
            options: import(".prisma/client").ModifierOption[];
        };
        minSelect: number;
        maxSelect: number | null;
        isRequired: boolean;
    }[]>;
}
//# sourceMappingURL=ListVariantModifierGroups.d.ts.map