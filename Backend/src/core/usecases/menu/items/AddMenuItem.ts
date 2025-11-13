import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';
import type { MenuItem } from '@prisma/client';

type AddMenuItemInput = {
  sectionId: number;
  refType: MenuItem['refType'];
  refId: number;
  displayName?: string | null;
  displayPriceCents?: number | null;
  position?: number;
  isFeatured?: boolean;
  isActive?: boolean;
};

export class AddMenuItem {
  constructor(private readonly repo: IMenuRepository) {}

  exec(input: AddMenuItemInput) {
    return this.repo.addItem(input);
  }
}
