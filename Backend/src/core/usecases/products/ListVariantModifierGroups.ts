import type { IProductRepository } from '../../domain/repositories/IProductRepository';

export class ListVariantModifierGroups {
  constructor(private readonly repo: IProductRepository) {}

  exec(variantId: number) {
    return this.repo.listVariantModifierGroups(variantId);
  }
}
