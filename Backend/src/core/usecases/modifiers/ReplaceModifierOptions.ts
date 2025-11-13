// src/core/usecases/modifiers/ReplaceModifierOptions.ts
import type { IModifierRepository } from '../../domain/repositories/IModifierRepository';
export class ReplaceModifierOptions {
  constructor(private repo: IModifierRepository) {}
  exec(groupId: number, options: { name: string; priceExtraCents?: number; isDefault?: boolean; position?: number }[]) {
    return this.repo.replaceOptions(groupId, options);
  }
}
