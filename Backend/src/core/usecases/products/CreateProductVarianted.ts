import type { IProductRepository } from '../../domain/repositories/IProductRepository';

export class CreateProductVarianted {
  constructor(private repo: IProductRepository) {}
  exec(input: {
    name: string;
    categoryId: number;
    variants: { name: string; priceCents: number; sku?: string }[];
    description?: string;
    sku?: string;
    image?: { buffer: Buffer; mimeType: string; size: number }; // 👈 EN VEZ DE imageUrl?
  }) {
    return this.repo.createVarianted(input);
  }
}
