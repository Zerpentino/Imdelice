import { IRoleRepository } from "../../domain/repositories/IRoleRepository";

export class CreateRole {
  constructor(private repo: IRoleRepository) {}
  async execute(input: { name: string; description?: string | null }) {
    // nombre único
    const dup = await this.repo.getByName(input.name);
    if (dup) {
      const e: any = new Error("El nombre del rol ya existe");
      e.status = 409;
      throw e;
    }
    return this.repo.create(input);
  }
}
