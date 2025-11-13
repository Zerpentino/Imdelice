import type { IProductRepository } from '../../domain/repositories/IProductRepository';

export class UpdateModifierGroupPosition {
  constructor(private repo: IProductRepository) {}
  exec(linkId: number, position: number) {
    return this.repo.updateModifierGroupPosition(linkId, position);
  }
}
