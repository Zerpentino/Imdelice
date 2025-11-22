import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export declare class CreateProductSimple {
    private repo;
    constructor(repo: IProductRepository);
    exec(input: {
        name: string;
        categoryId: number;
        priceCents: number;
        description?: string;
        sku?: string;
        barcode?: string | null;
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
        barcode: string | null;
        image: Uint8Array | null;
        imageMimeType: string | null;
        imageSize: number | null;
        isAvailable: boolean;
    }>;
}
//# sourceMappingURL=CreateProductSimple.d.ts.map