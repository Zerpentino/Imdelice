import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';

export class GetMenuPublic {
  constructor(private readonly repo: IMenuRepository) {}

  exec(id: number) {
    return this.repo.getMenuPublic(id);
  }
}
