"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.PrismaTableRepository = void 0;
const prisma_1 = require("../../lib/prisma");
class PrismaTableRepository {
    create(data) {
        return prisma_1.prisma.table.create({
            data: {
                name: data.name,
                seats: data.seats ?? null,
                isActive: data.isActive ?? true
            }
        });
    }
    list(options) {
        const includeInactive = options?.includeInactive ?? false;
        return prisma_1.prisma.table.findMany({
            where: includeInactive ? undefined : { isActive: true },
            orderBy: [{ name: 'asc' }]
        });
    }
    getById(id) {
        return prisma_1.prisma.table.findUnique({ where: { id } });
    }
    update(id, data) {
        return prisma_1.prisma.table.update({
            where: { id },
            data
        });
    }
    async deactivate(id) {
        await prisma_1.prisma.table.update({
            where: { id },
            data: { isActive: false }
        });
    }
    async deleteHard(id) {
        await prisma_1.prisma.table.delete({ where: { id } });
    }
}
exports.PrismaTableRepository = PrismaTableRepository;
//# sourceMappingURL=PrismaTableRepository.js.map