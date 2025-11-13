import type { IModifierRepository } from '../../domain/repositories/IModifierRepository';

export class ListModifierGroups {
  constructor(private repo: IModifierRepository) {}
  exec(filter?: { isActive?: boolean; categoryId?: number; categorySlug?: string; search?: string }) {
    return this.repo.listGroups(filter);
  }
}
