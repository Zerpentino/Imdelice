"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.PrismaProductRepository = void 0;
const client_1 = require("@prisma/client");
const prisma_1 = require("../../lib/prisma");
class PrismaProductRepository {
    createSimple(data) {
        return prisma_1.prisma.product.create({
            data: {
                name: data.name,
                categoryId: data.categoryId,
                type: 'SIMPLE',
                priceCents: data.priceCents,
                description: data.description,
                sku: data.sku,
                image: data.image?.buffer,
                imageMimeType: data.image?.mimeType,
                imageSize: data.image?.size,
            }
        });
    }
    createVarianted(data) {
        return prisma_1.prisma.product.create({
            data: {
                name: data.name,
                categoryId: data.categoryId,
                type: 'VARIANTED',
                description: data.description,
                sku: data.sku,
                image: data.image?.buffer,
                imageMimeType: data.image?.mimeType,
                imageSize: data.image?.size,
                variants: {
                    create: data.variants.map(v => ({
                        name: v.name, priceCents: v.priceCents, sku: v.sku
                    }))
                }
            }
        });
    }
    update(id, data) {
        return prisma_1.prisma.$transaction(async (tx) => {
            // 1) Saber qué tipo es el producto actual
            const current = await tx.product.findUnique({
                where: { id },
                select: { type: true }
            });
            if (!current)
                throw new Error('Product not found');
            // 2) Si el producto es COMBO y quieren cambiar la categoría, validar isComboOnly
            if (current.type === 'COMBO' && data.categoryId !== undefined) {
                const cat = await tx.category.findUnique({ where: { id: data.categoryId } });
                if (!cat)
                    throw new Error('Category not found');
                if (cat.isComboOnly !== true)
                    throw new Error('This category does not accept combos');
            }
            // 3) Actualización normal + manejo de imagen (undefined = no tocar; null = borrar; objeto = reemplazar)
            const updated = await tx.product.update({
                where: { id },
                data: {
                    ...data,
                    image: data.image === undefined ? undefined : (data.image ? data.image.buffer : null),
                    imageMimeType: data.image === undefined ? undefined : (data.image ? data.image.mimeType : null),
                    imageSize: data.image === undefined ? undefined : (data.image ? data.image.size : null),
                }
            });
            return updated;
        });
    }
    async replaceVariants(productId, variants) {
        await prisma_1.prisma.$transaction([
            prisma_1.prisma.productVariant.deleteMany({ where: { productId } }),
            prisma_1.prisma.product.update({
                where: { id: productId },
                data: { variants: { create: variants.map(v => ({ name: v.name, priceCents: v.priceCents, sku: v.sku })) } }
            })
        ]);
    }
    // NUEVO: conversiones
    async convertToVarianted(productId, variants) {
        await prisma_1.prisma.$transaction([
            prisma_1.prisma.product.update({ where: { id: productId }, data: { type: 'VARIANTED', priceCents: null } }),
            prisma_1.prisma.productVariant.deleteMany({ where: { productId } }),
            prisma_1.prisma.product.update({
                where: { id: productId },
                data: { variants: { create: variants.map(v => ({ name: v.name, priceCents: v.priceCents, sku: v.sku })) } }
            })
        ]);
    }
    async convertToSimple(productId, priceCents) {
        await prisma_1.prisma.$transaction([
            prisma_1.prisma.productVariant.deleteMany({ where: { productId } }),
            prisma_1.prisma.product.update({ where: { id: productId }, data: { type: 'SIMPLE', priceCents } })
        ]);
    }
    getById(id) {
        return prisma_1.prisma.product.findUnique({
            where: { id },
            include: {
                variants: {
                    orderBy: { position: 'asc' },
                    include: {
                        modifierGroupLinks: {
                            include: {
                                group: {
                                    include: { options: true }
                                }
                            }
                        }
                    }
                },
                modifierGroups: { include: { group: { include: { options: true } } }, orderBy: { position: 'asc' } },
                comboItemsAsCombo: {
                    include: {
                        componentProduct: { select: { id: true, name: true, type: true, priceCents: true, isActive: true } },
                        componentVariant: { select: { id: true, name: true, priceCents: true, isActive: true, productId: true } }
                    },
                    orderBy: { id: 'asc' }
                }
            }
        });
    }
    async list(filter) {
        const products = await prisma_1.prisma.product.findMany({
            where: {
                type: filter?.type,
                isActive: filter?.isActive,
                category: filter?.categorySlug ? { slug: filter.categorySlug } : undefined
            },
            orderBy: [{ createdAt: 'desc' }],
            select: {
                id: true,
                name: true,
                description: true,
                type: true,
                categoryId: true,
                priceCents: true,
                isActive: true,
                sku: true,
                imageMimeType: true,
                imageSize: true,
                isAvailable: true,
                createdAt: true,
                updatedAt: true
            }
        });
        return products.map(prod => ({
            ...prod,
            imageUrl: prod.imageMimeType ? `/products/${prod.id}/image?v=${prod.updatedAt.getTime()}` : null,
            hasImage: Boolean(prod.imageMimeType)
        }));
    }
    async attachModifierGroup(productId, groupId, position = 0) {
        await prisma_1.prisma.productModifierGroup.create({ data: { productId, groupId, position } });
    }
    async attachModifierGroupToVariant(variantId, data) {
        const createPayload = {
            variantId,
            groupId: data.groupId,
            minSelect: data.minSelect ?? 0,
            maxSelect: data.maxSelect ?? null,
            isRequired: data.isRequired ?? false
        };
        try {
            await prisma_1.prisma.productVariantModifierGroup.create({ data: createPayload });
        }
        catch (err) {
            if (err instanceof client_1.Prisma.PrismaClientKnownRequestError && err.code === 'P2002') {
                await this.updateVariantModifierGroup(variantId, data.groupId, data);
            }
            else {
                throw err;
            }
        }
    }
    async updateVariantModifierGroup(variantId, groupId, data) {
        const updateData = {};
        if (data.minSelect !== undefined)
            updateData.minSelect = data.minSelect;
        if (data.maxSelect !== undefined)
            updateData.maxSelect = data.maxSelect;
        if (data.isRequired !== undefined)
            updateData.isRequired = data.isRequired;
        if (!Object.keys(updateData).length)
            return;
        await prisma_1.prisma.productVariantModifierGroup.update({
            where: { variantId_groupId: { variantId, groupId } },
            data: updateData
        });
    }
    async detachModifierGroupFromVariant(variantId, groupId) {
        await prisma_1.prisma.productVariantModifierGroup.deleteMany({
            where: { variantId, groupId }
        });
    }
    async listVariantModifierGroups(variantId) {
        const links = await prisma_1.prisma.productVariantModifierGroup.findMany({
            where: { variantId },
            include: {
                group: { include: { options: true } }
            },
            orderBy: [{ groupId: 'asc' }]
        });
        return links.map(link => ({
            group: link.group,
            minSelect: link.minSelect,
            maxSelect: link.maxSelect,
            isRequired: link.isRequired
        }));
    }
    async deactivate(id) {
        await prisma_1.prisma.product.update({ where: { id }, data: { isActive: false } });
    }
    async deleteHard(id) {
        await prisma_1.prisma.$transaction(async (tx) => {
            const variants = await tx.productVariant.findMany({
                where: { productId: id },
                select: { id: true }
            });
            const variantIds = variants.map(v => v.id);
            await tx.productModifierGroup.deleteMany({ where: { productId: id } });
            await tx.comboItem.deleteMany({
                where: {
                    OR: [
                        { comboProductId: id },
                        { componentProductId: id },
                        { componentVariant: { productId: id } }
                    ]
                }
            });
            const menuItemFilters = [
                { refType: 'PRODUCT', refId: id },
                { refType: 'COMBO', refId: id }
            ];
            if (variantIds.length) {
                menuItemFilters.push({ refType: 'VARIANT', refId: { in: variantIds } });
            }
            await tx.menuItem.deleteMany({ where: { OR: menuItemFilters } });
            await tx.productVariant.deleteMany({ where: { productId: id } });
            await tx.product.delete({ where: { id } });
        });
    }
    //   -------------combos ---------------------
    async createCombo(data) {
        const cat = await prisma_1.prisma.category.findUnique({ where: { id: data.categoryId } });
        if (!cat)
            throw new Error('Category not found');
        if (cat.isComboOnly !== true)
            throw new Error('This category does not accept combos');
        const items = data.items ?? [];
        if (items.length) {
            const variantIds = items
                .map(it => it.componentVariantId)
                .filter((id) => typeof id === 'number');
            if (variantIds.length) {
                const variants = await prisma_1.prisma.productVariant.findMany({
                    where: { id: { in: variantIds } },
                    select: { id: true, productId: true }
                });
                const variantMap = new Map(variants.map(v => [v.id, v.productId]));
                for (const item of items) {
                    if (item.componentVariantId !== undefined) {
                        const ownerProductId = variantMap.get(item.componentVariantId);
                        if (!ownerProductId)
                            throw new Error('Component variant not found');
                        if (ownerProductId !== item.componentProductId) {
                            throw new Error('Component variant does not belong to provided product');
                        }
                    }
                }
            }
        }
        return prisma_1.prisma.product.create({
            data: {
                name: data.name,
                categoryId: data.categoryId,
                type: 'COMBO',
                priceCents: data.priceCents,
                description: data.description,
                sku: data.sku,
                image: data.image?.buffer,
                imageMimeType: data.image?.mimeType,
                imageSize: data.image?.size,
                comboItemsAsCombo: items.length ? {
                    create: items.map(it => ({
                        componentProductId: it.componentProductId,
                        componentVariantId: it.componentVariantId ?? null,
                        quantity: it.quantity ?? 1,
                        isRequired: it.isRequired ?? true,
                        notes: it.notes ?? null
                    }))
                } : undefined
            }
        });
    }
    async addComboItems(comboProductId, items) {
        if (!items.length)
            return;
        const variantIds = items
            .map(it => it.componentVariantId)
            .filter((id) => typeof id === 'number');
        if (variantIds.length) {
            const variants = await prisma_1.prisma.productVariant.findMany({
                where: { id: { in: variantIds } },
                select: { id: true, productId: true }
            });
            const variantMap = new Map(variants.map(v => [v.id, v.productId]));
            for (const item of items) {
                if (item.componentVariantId !== undefined) {
                    const ownerProductId = variantMap.get(item.componentVariantId);
                    if (!ownerProductId)
                        throw new Error('Component variant not found');
                    if (ownerProductId !== item.componentProductId) {
                        throw new Error('Component variant does not belong to provided product');
                    }
                }
            }
        }
        await prisma_1.prisma.comboItem.createMany({
            data: items.map(it => ({
                comboProductId,
                componentProductId: it.componentProductId,
                componentVariantId: it.componentVariantId ?? null,
                quantity: it.quantity ?? 1,
                isRequired: it.isRequired ?? true,
                notes: it.notes ?? null
            }))
        });
    }
    async updateComboItem(comboItemId, data) {
        await prisma_1.prisma.comboItem.update({
            where: { id: comboItemId },
            data: {
                quantity: data.quantity,
                isRequired: data.isRequired,
                notes: data.notes ?? null
            }
        });
    }
    async removeComboItem(comboItemId) {
        await prisma_1.prisma.comboItem.delete({ where: { id: comboItemId } });
    }
    // Quitar vínculo por linkId o por (productId, groupId)
    async detachModifierGroup(linkIdOr) {
        if ('linkId' in linkIdOr) {
            await prisma_1.prisma.productModifierGroup.delete({ where: { id: linkIdOr.linkId } });
        }
        else {
            await prisma_1.prisma.productModifierGroup.deleteMany({
                where: { productId: linkIdOr.productId, groupId: linkIdOr.groupId }
            });
        }
    }
    async updateModifierGroupPosition(linkId, position) {
        await prisma_1.prisma.productModifierGroup.update({
            where: { id: linkId },
            data: { position }
        });
    }
    async reorderModifierGroups(productId, items) {
        await prisma_1.prisma.$transaction(items.map(i => prisma_1.prisma.productModifierGroup.update({
            where: { id: i.linkId },
            data: { position: i.position }
        })));
    }
    // --- DISPONIBILIDAD (PRODUCTO y VARIANTE) ---
    async setProductAvailability(productId, isAvailable) {
        await prisma_1.prisma.product.update({
            where: { id: productId },
            data: { isAvailable }
        });
    }
    async setVariantAvailability(variantId, isAvailable, expectProductId) {
        // Validación opcional para asegurar pertenencia
        if (expectProductId !== undefined) {
            const variant = await prisma_1.prisma.productVariant.findUnique({
                where: { id: variantId },
                select: { id: true, productId: true }
            });
            if (!variant)
                throw new Error('Variant not found');
            if (variant.productId !== expectProductId) {
                throw new Error('Variant does not belong to the expected product');
            }
        }
        await prisma_1.prisma.productVariant.update({
            where: { id: variantId },
            data: { isAvailable }
        });
    }
}
exports.PrismaProductRepository = PrismaProductRepository;
//# sourceMappingURL=PrismaProductRepository.js.map