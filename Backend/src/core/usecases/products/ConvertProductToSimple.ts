import type { IProductRepository } from '../../domain/repositories/IProductRepository';

export class ConvertProductToSimple {
  constructor(private repo: IProductRepository) {}
  exec(productId: number, priceCents: number) {
    return this.repo.convertToSimple(productId, priceCents);
  }
}
