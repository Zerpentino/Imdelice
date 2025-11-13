import type { IProductRepository } from '../../domain/repositories/IProductRepository';
export class ListProducts {
  constructor(private repo: IProductRepository) {}
  exec(filter?: { categorySlug?: string; type?: 'SIMPLE'|'VARIANTED'|'COMBO'; isActive?: boolean }) {
    return this.repo.list(filter);
  }
}
