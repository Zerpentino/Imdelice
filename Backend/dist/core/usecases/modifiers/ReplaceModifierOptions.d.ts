import type { IModifierRepository } from '../../domain/repositories/IModifierRepository';
export declare class ReplaceModifierOptions {
    private repo;
    constructor(repo: IModifierRepository);
    exec(groupId: number, options: {
        name: string;
        priceExtraCents?: number;
        isDefault?: boolean;
        position?: number;
    }[]): Promise<void>;
}
//# sourceMappingURL=ReplaceModifierOptions.d.ts.map