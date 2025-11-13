import type { ITableRepository } from '../../domain/repositories/ITableRepository';
import type { Table } from '@prisma/client';

type UpdateTableInput = Partial<Pick<Table, 'name' | 'seats' | 'isActive'>>;

export class UpdateTable {
  constructor(private readonly repo: ITableRepository) {}

  exec(id: number, data: UpdateTableInput) {
    return this.repo.update(id, data);
  }
}
