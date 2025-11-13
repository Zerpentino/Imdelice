import { IUserRepository } from "../../domain/repositories/IUserRepository";

export class ListUsers {
  constructor(private repo: IUserRepository) {}
  async execute() {
    return this.repo.list();
  }
}
