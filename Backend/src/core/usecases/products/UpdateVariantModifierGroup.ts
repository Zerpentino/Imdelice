import type { IProductRepository } from '../../domain/repositories/IProductRepository';

export class UpdateVariantModifierGroup {
  constructor(private readonly repo: IProductRepository) {}

  exec(variantId: number, groupId: number, data: {
    minSelect?: number;
    maxSelect?: number | null;
    isRequired?: boolean;
  }) {
    return this.repo.updateVariantModifierGroup(variantId, groupId, data);
  }
}
