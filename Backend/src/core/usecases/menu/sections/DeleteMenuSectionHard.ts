import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';

export class DeleteMenuSectionHard {
  constructor(private readonly repo: IMenuRepository) {}

  exec(id: number) {
    return this.repo.deleteSectionHard(id);
  }
}
