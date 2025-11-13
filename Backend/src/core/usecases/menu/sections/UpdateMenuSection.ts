
import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';
import type { MenuSection } from '@prisma/client';

type UpdateSectionInput = Partial<
  Pick<MenuSection, 'name' | 'position' | 'isActive' | 'categoryId'>
>;

export class UpdateMenuSection {
  constructor(private readonly repo: IMenuRepository) {}

  exec(id: number, data: UpdateSectionInput) {
    return this.repo.updateSection(id, data);
  }
}
