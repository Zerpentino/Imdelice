import type { IModifierRepository } from '../../domain/repositories/IModifierRepository';

export class ListProductsByModifierGroup {
  constructor(private repo: IModifierRepository) {}
  exec(groupId: number, filter?: { isActive?: boolean; search?: string; limit?: number; offset?: number }) {
    return this.repo.listProductsByGroup(groupId, filter);
  }
}
