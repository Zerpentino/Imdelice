import { prisma } from "../../lib/prisma";
import { IRoleRepository, CreateRoleInput, UpdateRoleInput, RoleWithPermissions } from "../../core/domain/repositories/IRoleRepository";
import { Role } from "../../core/domain/entities/Role";

export class PrismaRoleRepository implements IRoleRepository {
  list(): Promise<Role[]> {
    return prisma.role.findMany();
  }
    async getById(id: number): Promise<RoleWithPermissions | null> {
    return prisma.role.findUnique({
      where: { id },
      include: {
        permissions: {
          include: { permission: true }
        }
      }
    });
  }

  getByName(name: string): Promise<Role | null> {
    return prisma.role.findUnique({ where: { name } });
  }
  create(input: CreateRoleInput): Promise<Role> {
    return prisma.role.create({ data: { name: input.name, description: input.description ?? null } });
  }
  update(input: UpdateRoleInput): Promise<Role> {
    const { id, name, description } = input;
    return prisma.role.update({
      where: { id },
      data: {
        ...(name !== undefined ? { name } : {}),
        ...(description !== undefined ? { description } : {}),
      },
    })
  }

  async delete(id: number): Promise<void> {
    await prisma.role.delete({ where: { id } });
  }
}
