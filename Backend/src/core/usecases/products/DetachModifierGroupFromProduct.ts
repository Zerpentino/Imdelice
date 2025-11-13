import type { IProductRepository } from '../../domain/repositories/IProductRepository';

export class DetachModifierGroupFromProduct {
  constructor(private repo: IProductRepository) {}
  // puedes pasar linkId o (productId, groupId)
  exec(linkIdOr: { linkId: number } | { productId: number; groupId: number }) {
    return this.repo.detachModifierGroup(linkIdOr);
  }
}
