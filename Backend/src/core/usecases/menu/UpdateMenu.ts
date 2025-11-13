import type { IMenuRepository } from '../../domain/repositories/IMenuRepository';
import type { Menu } from '@prisma/client';

type UpdateMenuInput = Partial<Pick<Menu, 'name' | 'isActive' | 'publishedAt' | 'version'>>;

export class UpdateMenu {
  constructor(private readonly repo: IMenuRepository) {}

  exec(id: number, data: UpdateMenuInput) {
    return this.repo.updateMenu(id, data);
  }
}
