import type { IMenuRepository } from '../../domain/repositories/IMenuRepository';

type CreateMenuInput = {
  name: string;
  isActive?: boolean;
  publishedAt?: Date | null;
  version?: number;
};

export class CreateMenu {
  constructor(private readonly repo: IMenuRepository) {}

  exec(input: CreateMenuInput) {
    return this.repo.createMenu(input);
  }
}
