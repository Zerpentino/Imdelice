import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';

export class DeleteMenuItemHard {
  constructor(private readonly repo: IMenuRepository) {}

  exec(id: number) {
    return this.repo.deleteItemHard(id);
  }
}
