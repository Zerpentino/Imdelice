import type { IProductRepository } from '../../domain/repositories/IProductRepository';

export class ReorderModifierGroups {
  constructor(private repo: IProductRepository) {}
  exec(productId: number, items: Array<{ linkId: number; position: number }>) {
    return this.repo.reorderModifierGroups(productId, items);
  }
}
