// src/core/usecases/categories/UpdateCategory.ts
import type { ICategoryRepository } from '../../domain/repositories/ICategoryRepository';
export class UpdateCategory {
  constructor(private repo: ICategoryRepository) {}
  exec(id: number, data: any) { return this.repo.update(id, data); }
}

