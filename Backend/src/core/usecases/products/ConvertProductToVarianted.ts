import type { IProductRepository } from '../../domain/repositories/IProductRepository';

export class ConvertProductToVarianted {
  constructor(private repo: IProductRepository) {}
  exec(productId: number, variants: { name: string; priceCents: number; sku?: string }[]) {
    return this.repo.convertToVarianted(productId, variants);
  }
}
