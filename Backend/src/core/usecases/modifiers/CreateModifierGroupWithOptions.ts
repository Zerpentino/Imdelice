import type { IModifierRepository } from '../../domain/repositories/IModifierRepository';
export class CreateModifierGroupWithOptions {
  constructor(private repo: IModifierRepository) {}
  exec(input: {
    name: string; description?: string; minSelect?: number; maxSelect?: number | null; isRequired?: boolean;
        appliesToCategoryId?: number | null;

    options: { name: string; priceExtraCents?: number; isDefault?: boolean; position?: number }[];
  }) {
    return this.repo.createGroupWithOptions(input);
  }
}
