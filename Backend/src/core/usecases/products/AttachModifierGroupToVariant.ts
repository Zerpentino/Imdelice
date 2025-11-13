import type { IProductRepository } from '../../domain/repositories/IProductRepository';

export class AttachModifierGroupToVariant {
  constructor(private readonly repo: IProductRepository) {}

  exec(input: {
    variantId: number;
    groupId: number;
    minSelect?: number;
    maxSelect?: number | null;
    isRequired?: boolean;
  }) {
    return this.repo.attachModifierGroupToVariant(input.variantId, {
      groupId: input.groupId,
      minSelect: input.minSelect,
      maxSelect: input.maxSelect,
      isRequired: input.isRequired
    });
  }
}
