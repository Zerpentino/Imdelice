import type { IProductRepository } from "../../domain/repositories/IProductRepository";

export class GetProductByBarcode {
  constructor(private repo: IProductRepository) {}

  exec(barcode: string) {
    return this.repo.findByBarcode(barcode);
  }
}
