import type { IProductRepository } from "../../domain/repositories/IProductRepository";
export declare class GetProductByBarcode {
    private repo;
    constructor(repo: IProductRepository);
    exec(barcode: string): Promise<import("../../domain/repositories/IProductRepository").ProductWithDetails | null>;
}
//# sourceMappingURL=GetProductByBarcode.d.ts.map