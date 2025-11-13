import { IRoleRepository } from "../../domain/repositories/IRoleRepository";

export class ListRoles {
  constructor(private repo: IRoleRepository) {}
  execute() {
    return this.repo.list();
  }
}
