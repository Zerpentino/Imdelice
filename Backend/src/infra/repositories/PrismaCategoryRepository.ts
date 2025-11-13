import type { ICategoryRepository } from '../../core/domain/repositories/ICategoryRepository';
import type { Category } from '@prisma/client';
import { prisma } from '../../lib/prisma';

export class PrismaCategoryRepository implements ICategoryRepository {
  create(data: { name: string; slug: string; parentId?: number|null; position?: number;    isComboOnly?: boolean;       // ✅ agregar
 }): Promise<Category> {
    return prisma.category.create({ data: {
      name: data.name, slug: data.slug, parentId: data.parentId ?? null, position: data.position ?? 0,            isComboOnly: data.isComboOnly ?? false,  // ✅ ahora sí se persiste
    // ✅ agregar

    }});
  }
  list(): Promise<Category[]> {
    return prisma.category.findMany({ orderBy: [{ parentId: 'asc' }, { position: 'asc' }, { name: 'asc' }] });
  }
  getBySlug(slug: string): Promise<Category | null> {
    return prisma.category.findUnique({ where: { slug } });
  }
  update(id: number, data: Partial<Pick<Category,'name'|'slug'|'parentId'|'position'|'isActive'>>): Promise<Category> {
    return prisma.category.update({ where: { id }, data });
  }
  async deactivate(id: number): Promise<void> {
    await prisma.category.update({ where: { id }, data: { isActive: false } });
  }
  async deleteHard(id: number): Promise<void> {
    await prisma.category.delete({ where: { id } });
  }
}
