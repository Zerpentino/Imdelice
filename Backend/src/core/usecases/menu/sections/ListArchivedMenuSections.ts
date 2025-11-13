import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';

export class ListArchivedMenuSections {
  constructor(private readonly repo: IMenuRepository) {}

  exec(menuId: number) {
    return this.repo.listArchivedSections(menuId);
  }
}
