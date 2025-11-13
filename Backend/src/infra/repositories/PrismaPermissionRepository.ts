// src/infra/repositories/PrismaPermissionRepository.ts
import { prisma } from '../../lib/prisma'
import { IPermissionRepository } from '../../core/domain/repositories/IPermissionRepository'
import { Permission } from '../../core/domain/entities/Permission'

export class PrismaPermissionRepository implements IPermissionRepository {
  list(): Promise<Permission[]> { return prisma.permission.findMany() }
  getByCodes(codes: string[]): Promise<Permission[]> {
    return prisma.permission.findMany({ where: { code: { in: codes }}})
  }
  async listByRole(roleId: number): Promise<Permission[]> {
    return prisma.permission.findMany({
      where: { roles: { some: { roleId } } }
    })
  }
  async replaceRolePermissions(roleId: number, codes: string[]) {
    const perms = await this.getByCodes(codes)
    await prisma.rolePermission.deleteMany({ where: { roleId } })
    if (perms.length) {
      await prisma.rolePermission.createMany({
        data: perms.map(p => ({ roleId, permissionId: p.id })), skipDuplicates: true
      })
    }
  }
}
