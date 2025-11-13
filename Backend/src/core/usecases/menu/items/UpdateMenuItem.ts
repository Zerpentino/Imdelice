import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';
import type { MenuItem } from '@prisma/client';

type UpdateMenuItemInput = Partial<
  Pick<MenuItem, 'displayName' | 'displayPriceCents' | 'position' | 'isFeatured' | 'isActive'>
>;

export class UpdateMenuItem {
  constructor(private readonly repo: IMenuRepository) {}

  exec(id: number, data: UpdateMenuItemInput) {
    return this.repo.updateItem(id, data);
  }
}
