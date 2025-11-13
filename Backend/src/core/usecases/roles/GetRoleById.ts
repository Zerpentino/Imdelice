import { IRoleRepository } from "../../domain/repositories/IRoleRepository";

export class GetRoleById {
  constructor(private repo: IRoleRepository) {}
  execute(id: number) {
    return this.repo.getById(id);
  }
}
