"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.PrismaModifierRepository = void 0;
const prisma_1 = require("../../lib/prisma");
class PrismaModifierRepository {
    async listGroups(filter) {
        return prisma_1.prisma.modifierGroup.findMany({
            where: {
                isActive: filter?.isActive,
                appliesToCategoryId: filter?.categoryId,
                appliesToCategory: filter?.categorySlug ? { slug: filter.categorySlug } : undefined,
                // ❌ NO usar { mode: 'insensitive' } en MySQL
                name: filter?.search ? { contains: filter.search } : undefined,
            },
            include: { options: true }, // 👈 NECESARIO para cumplir el contrato
            orderBy: [{ position: 'asc' }, { id: 'asc' }],
        });
    }
    getGroup(id) {
        return prisma_1.prisma.modifierGroup.findUnique({
            where: { id },
            include: { options: true }, // 👈 igual aquí
        });
    }
    async listByProduct(productId) {
        const links = await prisma_1.prisma.productModifierGroup.findMany({
            where: { productId },
            include: { group: { include: { options: true } } }, // 👈 incluir options
            orderBy: { position: 'asc' },
        });
        return links.map(l => ({ id: l.id, position: l.position, group: l.group }));
    }
    async createGroupWithOptions(data) {
        const group = await prisma_1.prisma.modifierGroup.create({
            data: {
                name: data.name, description: data.description,
                minSelect: data.minSelect ?? 0, maxSelect: data.maxSelect ?? null, isRequired: data.isRequired ?? false,
                appliesToCategoryId: data.appliesToCategoryId ?? null,
                options: { create: data.options.map(o => ({ name: o.name, priceExtraCents: o.priceExtraCents ?? 0, isDefault: o.isDefault ?? false, position: o.position ?? 0 })) }
            },
            include: { options: true }
        });
        return { group, options: group.options };
    }
    updateGroup(id, data) {
        return prisma_1.prisma.modifierGroup.update({ where: { id }, data });
    }
    async replaceOptions(groupId, options) {
        // 1) Traer las actuales
        const current = await prisma_1.prisma.modifierOption.findMany({ where: { groupId } });
        const currentByName = new Map(current.map(o => [o.name, o]));
        const payloadByName = new Map(options.map(o => [o.name, o]));
        // 2) Preparar operaciones
        const toUpdate = [];
        const toCreate = [];
        const candidatesToDelete = [];
        // a) Actualizar las que coinciden por nombre
        for (const o of options) {
            const existing = currentByName.get(o.name);
            if (existing) {
                toUpdate.push(prisma_1.prisma.modifierOption.update({
                    where: { id: existing.id },
                    data: {
                        priceExtraCents: o.priceExtraCents ?? 0,
                        isDefault: !!o.isDefault,
                        position: o.position ?? 0,
                        isActive: true,
                    },
                }));
            }
            else {
                toCreate.push({
                    groupId,
                    name: o.name,
                    priceExtraCents: o.priceExtraCents ?? 0,
                    isDefault: !!o.isDefault,
                    position: o.position ?? 0,
                    isActive: true,
                });
            }
        }
        // b) Las que ya no vienen en el payload
        for (const existing of current) {
            if (!payloadByName.has(existing.name)) {
                candidatesToDelete.push(existing.id);
            }
        }
        // 3) Ver cuáles tienen uso histórico en OrderItemModifier
        const used = await prisma_1.prisma.orderItemModifier.findMany({
            where: { optionId: { in: candidatesToDelete } },
            select: { optionId: true },
        });
        const usedSet = new Set(used.map(u => u.optionId));
        const toSoftDisable = candidatesToDelete.filter(id => usedSet.has(id));
        const toHardDelete = candidatesToDelete.filter(id => !usedSet.has(id));
        // 4) Ejecutar en transacción
        await prisma_1.prisma.$transaction([
            // actualizar
            ...toUpdate,
            // crear nuevas
            prisma_1.prisma.modifierOption.createMany({ data: toCreate }),
            // desactivar las con historial
            ...(toSoftDisable.length
                ? [prisma_1.prisma.modifierOption.updateMany({ where: { id: { in: toSoftDisable } }, data: { isActive: false } })]
                : []),
            // borrar las sin uso
            ...(toHardDelete.length
                ? [prisma_1.prisma.modifierOption.deleteMany({ where: { id: { in: toHardDelete } } })]
                : []),
        ]);
    }
    async updateOption(id, data) {
        return prisma_1.prisma.modifierOption.update({ where: { id }, data });
    }
    async deactivateGroup(id) {
        await prisma_1.prisma.modifierGroup.update({ where: { id }, data: { isActive: false } });
    }
    // src/infra/repositories/PrismaModifierRepository.ts
    async deleteGroupHard(id) {
        await prisma_1.prisma.$transaction(async (tx) => {
            // 1) Links a productos
            await tx.productModifierGroup.deleteMany({ where: { groupId: id } });
            // 2) Opciones del grupo
            await tx.modifierOption.deleteMany({ where: { groupId: id } });
            // 3) Borra el grupo
            await tx.modifierGroup.delete({ where: { id } });
        });
    }
    async listProductsByGroup(groupId, filter) {
        const links = await prisma_1.prisma.productModifierGroup.findMany({
            where: {
                groupId,
                product: {
                    isActive: filter?.isActive,
                    name: filter?.search ? { contains: filter.search } : undefined, // MySQL: no uses mode:'insensitive'
                },
            },
            include: {
                product: { select: { id: true, name: true, type: true, priceCents: true, isActive: true, categoryId: true } },
            },
            orderBy: [{ position: 'asc' }, { id: 'asc' }],
            take: filter?.limit ?? 50,
            skip: filter?.offset ?? 0,
        });
        return links.map(l => ({
            linkId: l.id,
            position: l.position,
            product: l.product,
        }));
    }
}
exports.PrismaModifierRepository = PrismaModifierRepository;
//# sourceMappingURL=PrismaModifierRepository.js.map