import { IUserRepository } from '../../domain/repositories/IUserRepository';
export class GetUserById {
  constructor(private repo: IUserRepository) {}
  execute(id: number) { return this.repo.getById(id); }
}
