// src/core/usecases/modifiers/DeleteModifierGroup.ts
import type { IModifierRepository } from '../../domain/repositories/IModifierRepository';
export class DeleteModifierGroup {
  constructor(private repo: IModifierRepository) {}
  async exec(id: number, hard = false) {
    return hard ? this.repo.deleteGroupHard(id) : this.repo.deactivateGroup(id);
  }
}