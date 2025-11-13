import type { ModifierGroup, ModifierOption } from '@prisma/client';
export interface IModifierRepository {
    createGroupWithOptions(data: {
        name: string;
        description?: string;
        minSelect?: number;
        maxSelect?: number | null;
        isRequired?: boolean;
        appliesToCategoryId?: number | null;
        options: {
            name: string;
            priceExtraCents?: number;
            isDefault?: boolean;
            position?: number;
        }[];
    }): Promise<{
        group: ModifierGroup;
        options: ModifierOption[];
    }>;
    updateGroup(id: number, data: Partial<Pick<ModifierGroup, 'name' | 'description' | 'minSelect' | 'maxSelect' | 'isRequired' | 'isActive' | 'appliesToCategoryId'>>): Promise<ModifierGroup>;
    replaceOptions(groupId: number, options: {
        name: string;
        priceExtraCents?: number;
        isDefault?: boolean;
        position?: number;
    }[]): Promise<void>;
    deactivateGroup(id: number): Promise<void>;
    deleteGroupHard(id: number): Promise<void>;
    updateOption(id: number, data: Partial<Pick<ModifierOption, 'name' | 'priceExtraCents' | 'isDefault' | 'position' | 'isActive'>>): Promise<ModifierOption>;
    listGroups(filter?: {
        isActive?: boolean;
        categoryId?: number;
        categorySlug?: string;
        search?: string;
    }): Promise<(ModifierGroup & {
        options: ModifierOption[];
    })[]>;
    getGroup(id: number): Promise<(ModifierGroup & {
        options: ModifierOption[];
    }) | null>;
    listByProduct(productId: number): Promise<{
        id: number;
        position: number;
        group: ModifierGroup & {
            options: ModifierOption[];
        };
    }[]>;
    listProductsByGroup(groupId: number, filter?: {
        isActive?: boolean;
        search?: string;
        limit?: number;
        offset?: number;
    }): Promise<Array<{
        linkId: number;
        position: number;
        product: {
            id: number;
            name: string;
            type: 'SIMPLE' | 'VARIANTED' | 'COMBO';
            priceCents: number | null;
            isActive: boolean;
            categoryId: number;
        };
    }>>;
}
//# sourceMappingURL=IModifierRepository.d.ts.map