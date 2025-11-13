// src/core/usecases/products/DeleteProduct.ts
import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export class DeleteProduct {
  constructor(private repo: IProductRepository) {}
  async exec(id: number, hard = false) {
    return hard ? this.repo.deleteHard(id) : this.repo.deactivate(id);
  }
}