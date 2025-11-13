import type { ICategoryRepository } from '../../domain/repositories/ICategoryRepository';

export class CreateCategory {
  constructor(private repo: ICategoryRepository) {}
  exec(input: { name: string; slug: string; parentId?: number | null; position?: number;    isComboOnly?: boolean;    // ✅ agregar
 }) {
    return this.repo.create(input);
  }
}
