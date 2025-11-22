import type { IProductRepository } from '../../domain/repositories/IProductRepository';

export class ConvertProductToVarianted {
  constructor(private repo: IProductRepository) {}
  exec(
    productId: number,
    variants: { name: string; priceCents: number; sku?: string; barcode?: string | null }[],
  ) {
    return this.repo.convertToVarianted(productId, variants);
  }
}
