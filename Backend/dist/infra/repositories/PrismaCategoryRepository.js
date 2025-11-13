"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.PrismaCategoryRepository = void 0;
const prisma_1 = require("../../lib/prisma");
class PrismaCategoryRepository {
    create(data) {
        return prisma_1.prisma.category.create({ data: {
                name: data.name, slug: data.slug, parentId: data.parentId ?? null, position: data.position ?? 0, isComboOnly: data.isComboOnly ?? false, // ✅ ahora sí se persiste
                // ✅ agregar
            } });
    }
    list() {
        return prisma_1.prisma.category.findMany({ orderBy: [{ parentId: 'asc' }, { position: 'asc' }, { name: 'asc' }] });
    }
    getBySlug(slug) {
        return prisma_1.prisma.category.findUnique({ where: { slug } });
    }
    update(id, data) {
        return prisma_1.prisma.category.update({ where: { id }, data });
    }
    async deactivate(id) {
        await prisma_1.prisma.category.update({ where: { id }, data: { isActive: false } });
    }
    async deleteHard(id) {
        await prisma_1.prisma.category.delete({ where: { id } });
    }
}
exports.PrismaCategoryRepository = PrismaCategoryRepository;
//# sourceMappingURL=PrismaCategoryRepository.js.map