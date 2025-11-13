import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export declare class CreateProductVarianted {
    private repo;
    constructor(repo: IProductRepository);
    exec(input: {
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
    }): Promise<{
        name: string;
        id: number;
        description: string | null;
        createdAt: Date;
        updatedAt: Date;
        type: import(".prisma/client").$Enums.ProductType;
        isActive: boolean;
        categoryId: number;
        priceCents: number | null;
        sku: string | null;
        image: Uint8Array | null;
        imageMimeType: string | null;
        imageSize: number | null;
        isAvailable: boolean;
    }>;
}
//# sourceMappingURL=CreateProductVarianted.d.ts.map