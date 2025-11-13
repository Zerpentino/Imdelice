import type { IModifierRepository } from '../../domain/repositories/IModifierRepository';

export class UpdateModifierOption {
  constructor(private repo: IModifierRepository) {}
  exec(id: number, data: { name?: string; priceExtraCents?: number; isDefault?: boolean; position?: number; isActive?: boolean }) {
    return this.repo.updateOption(id, data);
  }
}
