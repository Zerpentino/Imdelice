import type { ICategoryRepository } from '../../domain/repositories/ICategoryRepository';
export class ListCategories {
  constructor(private repo: ICategoryRepository) {}
  exec() { return this.repo.list(); }
}
