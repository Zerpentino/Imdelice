import type { IModifierRepository } from '../../domain/repositories/IModifierRepository';
export declare class ListModifierGroupsByProduct {
    private repo;
    constructor(repo: IModifierRepository);
    exec(productId: number): Promise<{
        id: number;
        position: number;
        group: import(".prisma/client").ModifierGroup & {
            options: import(".prisma/client").ModifierOption[];
        };
    }[]>;
}
//# sourceMappingURL=ListModifierGroupsByProduct.d.ts.map