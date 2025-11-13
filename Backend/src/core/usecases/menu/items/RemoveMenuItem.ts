import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';

export class RemoveMenuItem {
  constructor(private readonly repo: IMenuRepository) {}

  exec(id: number, hard = false) {
    return this.repo.removeItem(id, hard);
  }
}
