// src/core/usecases/categories/DeleteCategory.ts
import type { ICategoryRepository } from '../../domain/repositories/ICategoryRepository';
export class DeleteCategory {
  constructor(private repo: ICategoryRepository) {}
  async exec(id: number, hard = false) {
    return hard ? this.repo.deleteHard(id) : this.repo.deactivate(id);
  }
}
