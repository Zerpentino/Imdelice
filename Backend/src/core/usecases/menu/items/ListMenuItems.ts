import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';

export class ListMenuItems {
  constructor(private readonly repo: IMenuRepository) {}

  exec(sectionId: number) {
    return this.repo.listItems(sectionId);
  }
}
