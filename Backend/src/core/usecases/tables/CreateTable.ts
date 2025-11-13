import type { ITableRepository } from '../../domain/repositories/ITableRepository';

type CreateTableInput = {
  name: string;
  seats?: number | null;
  isActive?: boolean;
};

export class CreateTable {
  constructor(private readonly repo: ITableRepository) {}

  exec(input: CreateTableInput) {
    return this.repo.create(input);
  }
}
