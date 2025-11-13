import type { Product, ProductVariant, ModifierGroup, ModifierOption } from '@prisma/client';
export type ProductListItem = Omit<Product, 'image'> & {
    imageUrl: string | null;
    hasImage: boolean;
};
export type ProductWithDetails = Product & {
    variants: (ProductVariant & {
        modifierGroupLinks: {
            group: {
                id: number;
                name: string;
                minSelect: number;
                maxSelect: number | null;
                isRequired: boolean;
                options: {
                    id: number;
                    name: string;
                    priceExtraCents: number;
                    isDefault: boolean;
                }[];
            };
            minSelect: number;
            maxSelect: number | null;
            isRequired: boolean;
        }[];
    })[];
    modifierGroups: {
        id: number;
        position: number;
        group: {
            id: number;
            name: string;
            minSelect: number;
            maxSelect: number | null;
            isRequired: boolean;
            options: {
                id: number;
                name: string;
                priceExtraCents: number;
                isDefault: boolean;
            }[];
        };
    }[];
    comboItemsAsCombo: {
        id: number;
        quantity: number;
        isRequired: boolean;
        notes: string | null;
        componentProduct: {
            id: number;
            name: string;
            type: 'SIMPLE' | 'VARIANTED' | 'COMBO';
            priceCents: number | null;
            isActive: boolean;
        };
        componentVariant?: {
            id: number;
            name: string;
            priceCents: number;
            isActive: boolean;
            productId: number;
        } | null;
    }[];
};
export interface IProductRepository {
    createSimple(data: {
        name: string;
        categoryId: number;
        priceCents: number;
        description?: string;
        sku?: string;
        image?: {
            buffer: Buffer;
            mimeType: string;
            size: number;
        };
    }): Promise<Product>;
    createVarianted(data: {
        name: string;
        categoryId: number;
        variants: {
            name: string;
            priceCents: number;
            sku?: string;
        }[];
        description?: string;
        sku?: string;
        image?: {
            buffer: Buffer;
            mimeType: string;
            size: number;
        };
    }): Promise<Product>;
    update(id: number, data: Partial<Pick<Product, 'name' | 'categoryId' | 'priceCents' | 'description' | 'sku' | 'isActive'>> & {
        image?: {
            buffer: Buffer;
            mimeType: string;
            size: number;
        } | null;
    }): Promise<Product>;
    convertToVarianted(productId: number, variants: {
        name: string;
        priceCents: number;
        sku?: string;
    }[]): Promise<void>;
    convertToSimple(productId: number, priceCents: number): Promise<void>;
    replaceVariants(productId: number, variants: {
        name: string;
        priceCents: number;
        sku?: string;
    }[]): Promise<void>;
    getById(id: number): Promise<ProductWithDetails | null>;
    list(filter?: {
        categorySlug?: string;
        type?: 'SIMPLE' | 'VARIANTED' | 'COMBO';
        isActive?: boolean;
    }): Promise<ProductListItem[]>;
    attachModifierGroup(productId: number, groupId: number, position?: number): Promise<void>;
    attachModifierGroupToVariant(variantId: number, data: {
        groupId: number;
        minSelect?: number;
        maxSelect?: number | null;
        isRequired?: boolean;
    }): Promise<void>;
    updateVariantModifierGroup(variantId: number, groupId: number, data: {
        minSelect?: number;
        maxSelect?: number | null;
        isRequired?: boolean;
    }): Promise<void>;
    detachModifierGroupFromVariant(variantId: number, groupId: number): Promise<void>;
    listVariantModifierGroups(variantId: number): Promise<Array<{
        group: ModifierGroup & {
            options: ModifierOption[];
        };
        minSelect: number;
        maxSelect: number | null;
        isRequired: boolean;
    }>>;
    deactivate(id: number): Promise<void>;
    deleteHard(id: number): Promise<void>;
    createCombo(data: {
        name: string;
        categoryId: number;
        priceCents: number;
        description?: string;
        sku?: string;
        image?: {
            buffer: Buffer;
            mimeType: string;
            size: number;
        } | null;
        items?: {
            componentProductId: number;
            componentVariantId?: number;
            quantity?: number;
            isRequired?: boolean;
            notes?: string;
        }[];
    }): Promise<Product>;
    addComboItems(comboProductId: number, items: {
        componentProductId: number;
        componentVariantId?: number;
        quantity?: number;
        isRequired?: boolean;
        notes?: string;
    }[]): Promise<void>;
    updateComboItem(comboItemId: number, data: Partial<{
        quantity: number;
        isRequired: boolean;
        notes: string;
    }>): Promise<void>;
    removeComboItem(comboItemId: number): Promise<void>;
    setProductAvailability(productId: number, isAvailable: boolean): Promise<void>;
    setVariantAvailability(variantId: number, isAvailable: boolean, expectProductId?: number): Promise<void>;
    detachModifierGroup(linkIdOr: {
        linkId: number;
    } | {
        productId: number;
        groupId: number;
    }): Promise<void>;
    updateModifierGroupPosition(linkId: number, position: number): Promise<void>;
    reorderModifierGroups(productId: number, items: Array<{
        linkId: number;
        position: number;
    }>): Promise<void>;
}
//# sourceMappingURL=IProductRepository.d.ts.map