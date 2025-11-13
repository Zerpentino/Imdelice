import type { IModifierRepository } from '../../domain/repositories/IModifierRepository';
export declare class ListProductsByModifierGroup {
    private repo;
    constructor(repo: IModifierRepository);
    exec(groupId: number, filter?: {
        isActive?: boolean;
        search?: string;
        limit?: number;
        offset?: number;
    }): Promise<{
        linkId: number;
        position: number;
        product: {
            id: number;
            name: string;
            type: "SIMPLE" | "VARIANTED" | "COMBO";
            priceCents: number | null;
            isActive: boolean;
            categoryId: number;
        };
    }[]>;
}
//# sourceMappingURL=ListProductsByModifierGroup.d.ts.map