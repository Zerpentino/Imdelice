import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';

export class RestoreMenuSection {
  constructor(private readonly repo: IMenuRepository) {}

  exec(id: number) {
    return this.repo.restoreSection(id);
  }
}
