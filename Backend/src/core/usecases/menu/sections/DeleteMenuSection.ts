import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';

export class DeleteMenuSection {
  constructor(private readonly repo: IMenuRepository) {}

  exec(id: number, hard = false) {
    return this.repo.deleteSection(id, hard);
  }
}
