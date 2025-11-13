import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export declare class RemoveComboItem {
    private repo;
    constructor(repo: IProductRepository);
    exec(comboItemId: number): Promise<void>;
}
//# sourceMappingURL=RemoveComboItem.d.ts.map