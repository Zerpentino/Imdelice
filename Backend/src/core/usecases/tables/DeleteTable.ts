import type { ITableRepository } from '../../domain/repositories/ITableRepository';

export class DeleteTable {
  constructor(private readonly repo: ITableRepository) {}

  async exec(id: number, hard = false) {
    if (hard) {
      await this.repo.deleteHard(id);
      return;
    }

    await this.repo.deactivate(id);
  }
}
