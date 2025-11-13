import { IUserRepository } from '../../domain/repositories/IUserRepository';

export class DeleteUser {
  constructor(private repo: IUserRepository) {}
  async execute(id: number): Promise<void> {
    await this.repo.delete(id);
  }
}
