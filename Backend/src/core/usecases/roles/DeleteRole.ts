import { IRoleRepository } from "../../domain/repositories/IRoleRepository";

export class DeleteRole {
  constructor(private repo: IRoleRepository) {}
  async execute(id: number) {
    await this.repo.delete(id);
  }
}
