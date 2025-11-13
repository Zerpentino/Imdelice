"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.PrismaPermissionRepository = void 0;
// src/infra/repositories/PrismaPermissionRepository.ts
const prisma_1 = require("../../lib/prisma");
class PrismaPermissionRepository {
    list() { return prisma_1.prisma.permission.findMany(); }
    getByCodes(codes) {
        return prisma_1.prisma.permission.findMany({ where: { code: { in: codes } } });
    }
    async listByRole(roleId) {
        return prisma_1.prisma.permission.findMany({
            where: { roles: { some: { roleId } } }
        });
    }
    async replaceRolePermissions(roleId, codes) {
        const perms = await this.getByCodes(codes);
        await prisma_1.prisma.rolePermission.deleteMany({ where: { roleId } });
        if (perms.length) {
            await prisma_1.prisma.rolePermission.createMany({
                data: perms.map(p => ({ roleId, permissionId: p.id })), skipDuplicates: true
            });
        }
    }
}
exports.PrismaPermissionRepository = PrismaPermissionRepository;
//# sourceMappingURL=PrismaPermissionRepository.js.map