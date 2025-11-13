"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.PrismaRoleRepository = void 0;
const prisma_1 = require("../../lib/prisma");
class PrismaRoleRepository {
    list() {
        return prisma_1.prisma.role.findMany();
    }
    async getById(id) {
        return prisma_1.prisma.role.findUnique({
            where: { id },
            include: {
                permissions: {
                    include: { permission: true }
                }
            }
        });
    }
    getByName(name) {
        return prisma_1.prisma.role.findUnique({ where: { name } });
    }
    create(input) {
        return prisma_1.prisma.role.create({ data: { name: input.name, description: input.description ?? null } });
    }
    update(input) {
        const { id, name, description } = input;
        return prisma_1.prisma.role.update({
            where: { id },
            data: {
                ...(name !== undefined ? { name } : {}),
                ...(description !== undefined ? { description } : {}),
            },
        });
    }
    async delete(id) {
        await prisma_1.prisma.role.delete({ where: { id } });
    }
}
exports.PrismaRoleRepository = PrismaRoleRepository;
//# sourceMappingURL=PrismaRoleRepository.js.map