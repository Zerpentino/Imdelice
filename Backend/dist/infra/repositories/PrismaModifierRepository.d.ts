import type { IModifierRepository } from '../../core/domain/repositories/IModifierRepository';
import type { ModifierGroup, ModifierOption } from '@prisma/client';
import type { Product } from '@prisma/client';
export declare class PrismaModifierRepository implements IModifierRepository {
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
        id?: number;
        name: string;
        priceExtraCents?: number;
        isDefault?: boolean;
        position?: number;
    }[]): Promise<void>;
    updateOption(id: number, data: Partial<Pick<ModifierOption, 'name' | 'priceExtraCents' | 'isDefault' | 'position' | 'isActive'>>): Promise<ModifierOption>;
    deactivateGroup(id: number): Promise<void>;
    deleteGroupHard(id: number): Promise<void>;
    listProductsByGroup(groupId: number, filter?: {
        isActive?: boolean;
        search?: string;
        limit?: number;
        offset?: number;
    }): Promise<Array<{
        linkId: number;
        position: number;
        product: Pick<Product, 'id' | 'name' | 'type' | 'priceCents' | 'isActive' | 'categoryId'>;
    }>>;
}
//# sourceMappingURL=PrismaModifierRepository.d.ts.map