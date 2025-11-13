// src/core/usecases/products/UpdateProduct.ts
import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export class UpdateProduct {
  constructor(private repo: IProductRepository) {}
  exec(id: number, data: any) { return this.repo.update(id, data); }
}
