import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export class AttachModifierGroupToProduct {
  constructor(private repo: IProductRepository) {}
  exec(input: { productId: number; groupId: number; position?: number }) {
    return this.repo.attachModifierGroup(input.productId, input.groupId, input.position ?? 0);
  }
}
