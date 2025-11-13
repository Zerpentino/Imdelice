import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export class AddComboItems {
  constructor(private repo: IProductRepository) {}
  exec(
  comboProductId: number,
  items: {
    componentProductId: number;
    componentVariantId?: number;
    quantity?: number;
    isRequired?: boolean;
    notes?: string;
  }[]
) {
  return this.repo.addComboItems(comboProductId, items);
}

}
