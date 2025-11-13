import type { ITableRepository } from '../../domain/repositories/ITableRepository';

export class ListTables {
  constructor(private readonly repo: ITableRepository) {}

  exec(includeInactive = false) {
    return this.repo.list({ includeInactive });
  }
}
