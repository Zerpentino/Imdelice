import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';

export class ListMenuSections {
  constructor(private readonly repo: IMenuRepository) {}

  exec(menuId: number) {
    return this.repo.listSections(menuId);
  }
}
