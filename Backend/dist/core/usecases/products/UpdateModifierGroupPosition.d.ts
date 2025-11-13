import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export declare class UpdateModifierGroupPosition {
    private repo;
    constructor(repo: IProductRepository);
    exec(linkId: number, position: number): Promise<void>;
}
//# sourceMappingURL=UpdateModifierGroupPosition.d.ts.map