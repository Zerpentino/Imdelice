import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';

export class ListArchivedMenuItems {
  constructor(private readonly repo: IMenuRepository) {}

  exec(sectionId: number) {
    return this.repo.listArchivedItems(sectionId);
  }
}
