import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';

type CreateSectionInput = {
  menuId: number;
  name: string;
  position?: number;
  categoryId?: number | null;
  isActive?: boolean;
};

export class CreateMenuSection {
  constructor(private readonly repo: IMenuRepository) {}

  exec(input: CreateSectionInput) {
    return this.repo.createSection(input);
  }
}
