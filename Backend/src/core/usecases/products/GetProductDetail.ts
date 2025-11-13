import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export class GetProductDetail {
  constructor(private repo: IProductRepository) {}
  exec(id: number) { return this.repo.getById(id); }
}
