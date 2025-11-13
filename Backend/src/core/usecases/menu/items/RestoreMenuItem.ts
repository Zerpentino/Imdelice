import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';

export class RestoreMenuItem {
  constructor(private readonly repo: IMenuRepository) {}

  exec(id: number) {
    return this.repo.restoreItem(id);
  }
}
