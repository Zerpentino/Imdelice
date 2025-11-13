import type { IMenuRepository } from '../../domain/repositories/IMenuRepository';

export class ListArchivedMenus {
  constructor(private readonly repo: IMenuRepository) {}

  exec() {
    return this.repo.listArchivedMenus();
  }
}
