import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export declare class UpdateComboItem {
    private repo;
    constructor(repo: IProductRepository);
    exec(comboItemId: number, data: Partial<{
        quantity: number;
        isRequired: boolean;
        notes: string;
    }>): Promise<void>;
}
//# sourceMappingURL=UpdateComboItem.d.ts.map