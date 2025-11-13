import type { IProductRepository } from '../../domain/repositories/IProductRepository';

export class CreateProductSimple {
  constructor(private repo: IProductRepository) {}
  exec(input: {
    name: string;
    categoryId: number;
    priceCents: number;
    description?: string;
    sku?: string;
    image?: { buffer: Buffer; mimeType: string; size: number }; // 👈 AQUI
  }) {
    return this.repo.createSimple(input);
  }
}
