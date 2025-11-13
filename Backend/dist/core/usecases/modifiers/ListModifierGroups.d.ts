import type { IModifierRepository } from '../../domain/repositories/IModifierRepository';
export declare class ListModifierGroups {
    private repo;
    constructor(repo: IModifierRepository);
    exec(filter?: {
        isActive?: boolean;
        categoryId?: number;
        categorySlug?: string;
        search?: string;
    }): Promise<({
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
    })[]>;
}
//# sourceMappingURL=ListModifierGroups.d.ts.map