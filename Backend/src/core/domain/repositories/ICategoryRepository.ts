import type { Category } from '@prisma/client';

export interface ICategoryRepository {
  create(data: { name: string; slug: string; parentId?: number | null; position?: number;    isComboOnly?: boolean;       // ✅ agregar
 }): Promise<Category>;
  list(): Promise<Category[]>;
  getBySlug(slug: string): Promise<Category | null>;
  update(id: number, data: Partial<Pick<Category,'name'|'slug'|'parentId'|'position'|'isActive'>>): Promise<Category>;
  deactivate(id: number): Promise<void>;     // soft delete
  deleteHard(id: number): Promise<void>;     // hard delete (FK ojo)
}
