import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export class RemoveComboItem {
  constructor(private repo: IProductRepository) {}
  exec(comboItemId: number) { return this.repo.removeComboItem(comboItemId); }
}
