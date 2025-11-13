import type { IModifierRepository } from '../../domain/repositories/IModifierRepository';
export declare class UpdateModifierOption {
    private repo;
    constructor(repo: IModifierRepository);
    exec(id: number, data: {
        name?: string;
        priceExtraCents?: number;
        isDefault?: boolean;
        position?: number;
        isActive?: boolean;
    }): Promise<{
        name: string;
        id: number;
        position: number;
        isActive: boolean;
        groupId: number;
        priceExtraCents: number;
        isDefault: boolean;
    }>;
}
//# sourceMappingURL=UpdateModifierOption.d.ts.map