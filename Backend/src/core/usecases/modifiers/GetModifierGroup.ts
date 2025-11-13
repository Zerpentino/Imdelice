import type { IModifierRepository } from '../../domain/repositories/IModifierRepository';

export class GetModifierGroup {
  constructor(private repo: IModifierRepository) {}
  exec(id: number) { return this.repo.getGroup(id); }
}
