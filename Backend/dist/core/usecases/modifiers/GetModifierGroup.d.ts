import type { IModifierRepository } from '../../domain/repositories/IModifierRepository';
export declare class GetModifierGroup {
    private repo;
    constructor(repo: IModifierRepository);
    exec(id: number): Promise<({
        name: string;
        id: number;
        description: string | null;
        position: number;
        isActive: boolean;
        minSelect: number;
        maxSelect: number | null;
        isRequired: boolean;
        appliesToCategoryId: number | null;
    } & {
        options: import(".prisma/client").ModifierOption[];
    }) | null>;
}
//# sourceMappingURL=GetModifierGroup.d.ts.map