import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export declare class AddComboItems {
    private repo;
    constructor(repo: IProductRepository);
    exec(comboProductId: number, items: {
        componentProductId: number;
        componentVariantId?: number;
        quantity?: number;
        isRequired?: boolean;
        notes?: string;
    }[]): Promise<void>;
}
//# sourceMappingURL=AddComboItems.d.ts.map