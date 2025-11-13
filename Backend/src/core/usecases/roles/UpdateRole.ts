import { IRoleRepository } from "../../domain/repositories/IRoleRepository";

export class UpdateRole {
  constructor(private repo: IRoleRepository) {}
  async execute(input: { id: number; name?: string; description?: string | null }) {
    // si cambia el nombre, valida duplicado
    if (input.name) {
      const existing = await this.repo.getByName(input.name);
      if (existing && existing.id !== input.id) {
        const e: any = new Error("El nombre del rol ya existe");
        e.status = 409;
        throw e;
      }
    }
    return this.repo.update(input);
  }
}
