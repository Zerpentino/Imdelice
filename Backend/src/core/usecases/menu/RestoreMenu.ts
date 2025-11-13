import type { IMenuRepository } from '../../domain/repositories/IMenuRepository';

export class RestoreMenu {
  constructor(private readonly repo: IMenuRepository) {}

  exec(id: number) {
    return this.repo.restoreMenu(id);
  }
}
