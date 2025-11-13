import type { IMenuRepository } from '../../domain/repositories/IMenuRepository';

export class ListMenus {
  constructor(private readonly repo: IMenuRepository) {}

  exec() {
    return this.repo.listMenus();
  }
}
