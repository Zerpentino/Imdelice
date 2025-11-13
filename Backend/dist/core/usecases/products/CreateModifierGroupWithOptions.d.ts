import type { IModifierRepository } from '../../domain/repositories/IModifierRepository';
export declare class CreateModifierGroupWithOptions {
    private repo;
    constructor(repo: IModifierRepository);
    exec(input: {
        name: string;
        description?: string;
        minSelect?: number;
        maxSelect?: number | null;
        isRequired?: boolean;
        options: {
            name: string;
            priceExtraCents?: number;
            isDefault?: boolean;
            position?: number;
        }[];
    }): Promise<{
        group: import(".prisma/client").ModifierGroup;
        options: import(".prisma/client").ModifierOption[];
    }>;
}
//# sourceMappingURL=CreateModifierGroupWithOptions.d.ts.map