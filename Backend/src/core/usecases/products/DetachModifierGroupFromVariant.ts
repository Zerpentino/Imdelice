import type { IProductRepository } from '../../domain/repositories/IProductRepository';

export class DetachModifierGroupFromVariant {
  constructor(private readonly repo: IProductRepository) {}

  exec(variantId: number, groupId: number) {
    return this.repo.detachModifierGroupFromVariant(variantId, groupId);
  }
}
