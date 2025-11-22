
// src/core/usecases/products/ReplaceProductVariants.ts
import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export class ReplaceProductVariants {
  constructor(private repo: IProductRepository) {}
  exec(productId: number, variants: { name: string; priceCents: number; sku?: string; barcode?: string | null }[]) {
    return this.repo.replaceVariants(productId, variants);
  }
}
