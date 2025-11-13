import type { ITableRepository } from '../../domain/repositories/ITableRepository';

export class GetTable {
  constructor(private readonly repo: ITableRepository) {}

  exec(id: number) {
    return this.repo.getById(id);
  }
}
