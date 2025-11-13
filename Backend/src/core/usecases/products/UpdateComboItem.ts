import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export class UpdateComboItem {
  constructor(private repo: IProductRepository) {}
  exec(comboItemId: number, data: Partial<{ quantity: number; isRequired: boolean; notes: string }>) {
    return this.repo.updateComboItem(comboItemId, data);
  }
}
