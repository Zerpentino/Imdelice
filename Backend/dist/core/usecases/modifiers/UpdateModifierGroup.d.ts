import type { IModifierRepository } from '../../domain/repositories/IModifierRepository';
type GroupEditable = {
    name?: string;
    description?: string | null;
    minSelect?: number;
    maxSelect?: number | null;
    isRequired?: boolean;
    isActive?: boolean;
    appliesToCategoryId?: number | null;
};
type UpdatePayload = GroupEditable & {
    replaceOptions?: Array<{
        name: string;
        priceExtraCents?: number;
        isDefault?: boolean;
        position?: number;
    }>;
};
export declare class UpdateModifierGroup {
    private repo;
    constructor(repo: IModifierRepository);
    exec(id: number, data: UpdatePayload): Promise<{
        name: string;
        id: number;
        description: string | null;
        position: number;
        isActive: boolean;
        minSelect: number;
        maxSelect: number | null;
        isRequired: boolean;
        appliesToCategoryId: number | null;
    }>;
}
export {};
//# sourceMappingURL=UpdateModifierGroup.d.ts.map