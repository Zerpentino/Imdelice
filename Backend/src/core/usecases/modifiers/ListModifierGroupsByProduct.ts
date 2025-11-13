import type { IModifierRepository } from '../../domain/repositories/IModifierRepository';

export class ListModifierGroupsByProduct {
  constructor(private repo: IModifierRepository) {}
  exec(productId: number) { return this.repo.listByProduct(productId); }
}
