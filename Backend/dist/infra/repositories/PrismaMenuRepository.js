"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.PrismaMenuRepository = void 0;
const client_1 = require("@prisma/client");
const prisma_1 = require("../../lib/prisma");
class PrismaMenuRepository {
    async createMenu(data) {
        return prisma_1.prisma.menu.create({
            data: {
                name: data.name,
                isActive: data.isActive ?? true,
                publishedAt: data.publishedAt ?? null,
                version: data.version ?? 1
            }
        });
    }
    listMenus() {
        return prisma_1.prisma.menu.findMany({
            where: { deletedAt: null },
            orderBy: [{ isActive: 'desc' }, { name: 'asc' }]
        });
    }
    listArchivedMenus() {
        return prisma_1.prisma.menu.findMany({
            where: { deletedAt: { not: null } },
            orderBy: [{ deletedAt: 'desc' }]
        });
    }
    async updateMenu(id, data) {
        await this.ensureMenuActive(id);
        return prisma_1.prisma.menu.update({ where: { id }, data });
    }
    async deleteMenu(id, hard) {
        const menu = await prisma_1.prisma.menu.findUnique({ where: { id } });
        if (!menu)
            throw new Error('Menu not found');
        if (hard) {
            if (menu.publishedAt && !menu.deletedAt) {
                throw new Error('Cannot hard delete a published menu. Unpublish first.');
            }
            await prisma_1.prisma.$transaction(async (tx) => {
                const sections = await tx.menuSection.findMany({ where: { menuId: id }, select: { id: true } });
                const sectionIds = sections.map((s) => s.id);
                if (sectionIds.length) {
                    await tx.menuItem.deleteMany({ where: { sectionId: { in: sectionIds } } });
                }
                await tx.menuSection.deleteMany({ where: { menuId: id } });
                await tx.menu.delete({ where: { id } });
            });
            return;
        }
        if (menu.deletedAt) {
            throw new Error('Menu already archived');
        }
        const now = new Date();
        await prisma_1.prisma.$transaction([
            prisma_1.prisma.menu.update({
                where: { id },
                data: { isActive: false, deletedAt: now, publishedAt: null }
            }),
            prisma_1.prisma.menuSection.updateMany({
                where: { menuId: id, deletedAt: null },
                data: { isActive: false, deletedAt: now }
            }),
            prisma_1.prisma.menuItem.updateMany({
                where: { section: { menuId: id }, deletedAt: null },
                data: { isActive: false, deletedAt: now }
            })
        ]);
    }
    async restoreMenu(id) {
        const archivedMenu = await prisma_1.prisma.menu.findFirst({
            where: { id, deletedAt: { not: null } }
        });
        if (!archivedMenu) {
            throw new Error('Menu is not archived');
        }
        await prisma_1.prisma.$transaction([
            prisma_1.prisma.menu.update({
                where: { id },
                data: { isActive: true, deletedAt: null }
            }),
            prisma_1.prisma.menuSection.updateMany({
                where: { menuId: id, deletedAt: { not: null } },
                data: { isActive: true, deletedAt: null }
            }),
            prisma_1.prisma.menuItem.updateMany({
                where: { section: { menuId: id }, deletedAt: { not: null } },
                data: { isActive: true, deletedAt: null }
            })
        ]);
    }
    async createSection(data) {
        await this.ensureMenuActive(data.menuId);
        await this.ensureCategoryExists(data.categoryId);
        return prisma_1.prisma.menuSection.create({
            data: {
                menuId: data.menuId,
                name: data.name,
                position: data.position ?? 0,
                isActive: data.isActive ?? true,
                categoryId: data.categoryId ?? null
            }
        });
    }
    async updateSection(id, data) {
        await this.ensureSectionActive(id);
        if (data.categoryId !== undefined) {
            await this.ensureCategoryExists(data.categoryId);
        }
        const updateData = {};
        if (data.name !== undefined)
            updateData.name = data.name;
        if (data.position !== undefined)
            updateData.position = data.position;
        if (data.isActive !== undefined)
            updateData.isActive = data.isActive;
        if (data.categoryId !== undefined)
            updateData.categoryId = data.categoryId;
        return prisma_1.prisma.menuSection.update({
            where: { id },
            data: updateData
        });
    }
    async deleteSection(id, hard) {
        const section = await prisma_1.prisma.menuSection.findUnique({ where: { id }, include: { menu: true } });
        if (!section || section.deletedAt)
            throw new Error('Menu section not found or already deleted');
        if (!section.menu || section.menu.deletedAt) {
            throw new Error('Parent menu is not active');
        }
        if (hard) {
            if (section.menu.publishedAt) {
                throw new Error('Cannot hard delete a section from a published menu. Unpublish first.');
            }
            await prisma_1.prisma.$transaction([
                prisma_1.prisma.menuItem.deleteMany({ where: { sectionId: id } }),
                prisma_1.prisma.menuSection.delete({ where: { id } })
            ]);
            return;
        }
        const now = new Date();
        await prisma_1.prisma.$transaction([
            prisma_1.prisma.menuSection.update({
                where: { id },
                data: { isActive: false, deletedAt: now }
            }),
            prisma_1.prisma.menuItem.updateMany({
                where: { sectionId: id, deletedAt: null },
                data: { isActive: false, deletedAt: now }
            })
        ]);
    }
    async restoreSection(id) {
        const section = await prisma_1.prisma.menuSection.findFirst({ where: { id, deletedAt: { not: null } } });
        if (!section)
            throw new Error('Menu section is not archived');
        await prisma_1.prisma.$transaction([
            prisma_1.prisma.menuSection.update({
                where: { id },
                data: { isActive: true, deletedAt: null }
            }),
            prisma_1.prisma.menuItem.updateMany({
                where: { sectionId: id, deletedAt: { not: null } },
                data: { isActive: true, deletedAt: null }
            })
        ]);
    }
    async deleteSectionHard(id) {
        await prisma_1.prisma.$transaction([
            prisma_1.prisma.menuItem.deleteMany({ where: { sectionId: id } }),
            prisma_1.prisma.menuSection.delete({ where: { id } })
        ]);
    }
    listSections(menuId) {
        return prisma_1.prisma.menuSection.findMany({
            where: { menuId, deletedAt: null },
            orderBy: { position: 'asc' },
            include: {
                items: {
                    where: { deletedAt: null },
                    orderBy: { position: 'asc' }
                }
            }
        });
    }
    listArchivedSections(menuId) {
        return prisma_1.prisma.menuSection.findMany({
            where: { menuId, deletedAt: { not: null } },
            orderBy: { deletedAt: 'desc' },
            include: {
                items: {
                    where: { deletedAt: { not: null } },
                    orderBy: { deletedAt: 'desc' }
                }
            }
        });
    }
    async addItem(data) {
        await this.ensureSectionActive(data.sectionId);
        const ref = await this.ensureRefExists(data.refType, data.refId);
        if (data.refType === 'COMBO' || data.refType === 'PRODUCT') {
            this.assertProductCanBeListed(ref, data.refType);
        }
        return prisma_1.prisma.menuItem.create({
            data: {
                sectionId: data.sectionId,
                refType: data.refType,
                refId: data.refId,
                displayName: data.displayName ?? null,
                displayPriceCents: data.displayPriceCents ?? null,
                position: data.position ?? 0,
                isFeatured: data.isFeatured ?? false,
                isActive: data.isActive ?? true
            }
        });
    }
    async updateItem(id, data) {
        await this.ensureItemActive(id);
        return prisma_1.prisma.menuItem.update({ where: { id }, data });
    }
    async removeItem(id, hard) {
        const item = await prisma_1.prisma.menuItem.findUnique({
            where: { id },
            include: { section: { include: { menu: true } } }
        });
        if (!item || item.deletedAt)
            throw new Error('Menu item not found or already deleted');
        if (!item.section || !item.section.menu || item.section.menu.deletedAt) {
            throw new Error('Parent menu is not active');
        }
        if (hard) {
            if (item.section.menu.publishedAt) {
                throw new Error('Cannot hard delete an item from a published menu. Unpublish first.');
            }
            await prisma_1.prisma.menuItem.delete({ where: { id } });
            return;
        }
        await prisma_1.prisma.menuItem.update({
            where: { id },
            data: { isActive: false, deletedAt: new Date() }
        });
    }
    async restoreItem(id) {
        const item = await prisma_1.prisma.menuItem.findFirst({ where: { id, deletedAt: { not: null } } });
        if (!item)
            throw new Error('Menu item is not archived');
        await prisma_1.prisma.menuItem.update({
            where: { id },
            data: { isActive: true, deletedAt: null }
        });
    }
    async deleteItemHard(id) {
        await prisma_1.prisma.menuItem.delete({ where: { id } });
    }
    async listItems(sectionId) {
        const items = await prisma_1.prisma.menuItem.findMany({
            where: { sectionId, deletedAt: null },
            orderBy: { position: 'asc' }
        });
        if (!items.length)
            return [];
        const resolved = await this.resolveMenuItemRefs(items);
        return items
            .map((item) => resolved.get(item.id))
            .filter((item) => Boolean(item));
    }
    async listArchivedItems(sectionId) {
        const items = await prisma_1.prisma.menuItem.findMany({
            where: { sectionId, deletedAt: { not: null } },
            orderBy: { deletedAt: 'desc' }
        });
        if (!items.length)
            return [];
        const resolved = await this.resolveMenuItemRefs(items);
        return items
            .map((item) => resolved.get(item.id))
            .filter((item) => Boolean(item));
    }
    async getMenuPublic(id) {
        const menu = await prisma_1.prisma.menu.findFirst({
            where: {
                id,
                deletedAt: null,
                isActive: true,
                OR: [
                    { publishedAt: null },
                    { publishedAt: { lte: new Date() } }
                ]
            },
            include: {
                sections: {
                    where: { deletedAt: null, isActive: true },
                    orderBy: { position: 'asc' },
                    include: {
                        items: {
                            where: { deletedAt: null, isActive: true },
                            orderBy: { position: 'asc' }
                        }
                    }
                }
            }
        });
        if (!menu)
            return null;
        const allItems = menu.sections.flatMap((section) => section.items);
        const resolved = await this.resolveMenuItemRefs(allItems);
        const sections = menu.sections
            .map((section) => ({
            ...section,
            items: section.items
                .map((item) => resolved.get(item.id))
                .filter((item) => Boolean(item))
        }))
            .filter((section) => section.items.length > 0);
        if (!sections.length)
            return null;
        return { ...menu, sections };
    }
    async ensureMenuActive(id) {
        const menu = await prisma_1.prisma.menu.findFirst({ where: { id, deletedAt: null } });
        if (!menu)
            throw new Error('Menu not found or inactive');
        return menu;
    }
    async ensureSectionActive(id) {
        const section = await prisma_1.prisma.menuSection.findFirst({
            where: { id, deletedAt: null },
            include: { menu: true }
        });
        if (!section || !section.menu || section.menu.deletedAt) {
            throw new Error('Menu section not found or inactive');
        }
        return section;
    }
    async ensureItemActive(id) {
        const item = await prisma_1.prisma.menuItem.findFirst({
            where: { id, deletedAt: null },
            include: { section: { include: { menu: true } } }
        });
        if (!item || !item.section || !item.section.menu || item.section.menu.deletedAt) {
            throw new Error('Menu item not found or inactive');
        }
        return item;
    }
    async ensureCategoryExists(categoryId) {
        if (!categoryId)
            return;
        const category = await prisma_1.prisma.category.findUnique({ where: { id: categoryId } });
        if (!category)
            throw new Error('Category does not exist');
    }
    assertProductCanBeListed(product, type) {
        if (type === 'COMBO' && product.type !== client_1.ProductType.COMBO) {
            throw new Error('Referenced product is not a combo');
        }
        if (type === 'PRODUCT' && product.type === client_1.ProductType.COMBO) {
            throw new Error('Referenced product is a combo, use refType COMBO');
        }
        if (!product.isActive || !product.isAvailable) {
            throw new Error('Referenced product is not active or available');
        }
    }
    async ensureRefExists(refType, refId) {
        switch (refType) {
            case 'PRODUCT':
            case 'COMBO': {
                const product = await prisma_1.prisma.product.findUnique({ where: { id: refId } });
                if (!product)
                    throw new Error('Referenced product not found');
                if (!product.isActive || !product.isAvailable) {
                    throw new Error('Referenced product is not active or available');
                }
                return product;
            }
            case 'VARIANT': {
                const variant = await prisma_1.prisma.productVariant.findUnique({
                    where: { id: refId },
                    include: { product: true }
                });
                if (!variant || !variant.product) {
                    throw new Error('Referenced variant not found');
                }
                if (!variant.isActive || !variant.isAvailable) {
                    throw new Error('Referenced variant is not active or available');
                }
                if (!variant.product.isActive || !variant.product.isAvailable) {
                    throw new Error('Parent product for variant is not active or available');
                }
                return variant;
            }
            case 'OPTION': {
                const option = await prisma_1.prisma.modifierOption.findUnique({ where: { id: refId } });
                if (!option)
                    throw new Error('Referenced modifier option not found');
                if (!option.isActive)
                    throw new Error('Modifier option is not active');
                return option;
            }
            default:
                throw new Error(`Unsupported refType: ${refType}`);
        }
    }
    async resolveMenuItemRefs(items) {
        const grouped = new Map();
        for (const item of items) {
            if (!grouped.has(item.refType))
                grouped.set(item.refType, []);
            grouped.get(item.refType)?.push(item.refId);
        }
        const result = new Map();
        const productIds = grouped.get('PRODUCT') ?? [];
        const comboIds = grouped.get('COMBO') ?? [];
        const variantIds = grouped.get('VARIANT') ?? [];
        const optionIds = grouped.get('OPTION') ?? [];
        const productSelect = {
            id: true,
            name: true,
            type: true,
            priceCents: true,
            isActive: true,
            isAvailable: true,
            imageMimeType: true,
            updatedAt: true
        };
        const productBasicSelect = {
            id: true,
            name: true,
            type: true,
            priceCents: true,
            isActive: true,
            isAvailable: true,
            imageMimeType: true,
            updatedAt: true
        };
        const comboSelect = {
            id: true,
            name: true,
            type: true,
            priceCents: true,
            isActive: true,
            isAvailable: true,
            imageMimeType: true,
            updatedAt: true,
            comboItemsAsCombo: {
                select: {
                    quantity: true,
                    isRequired: true,
                    notes: true,
                    componentProduct: { select: productBasicSelect },
                    componentVariant: {
                        select: {
                            id: true,
                            name: true,
                            priceCents: true,
                            isActive: true,
                            isAvailable: true
                        }
                    }
                }
            }
        };
        const [products, combos, variants, options] = await Promise.all([
            productIds.length
                ? prisma_1.prisma.product.findMany({
                    where: {
                        id: { in: productIds },
                        isActive: true,
                        isAvailable: true,
                        type: { not: client_1.ProductType.COMBO }
                    },
                    select: productSelect
                })
                : Promise.resolve([]),
            comboIds.length
                ? prisma_1.prisma.product.findMany({
                    where: {
                        id: { in: comboIds },
                        isActive: true,
                        isAvailable: true,
                        type: client_1.ProductType.COMBO
                    },
                    select: comboSelect
                })
                : Promise.resolve([]),
            variantIds.length
                ? prisma_1.prisma.productVariant.findMany({
                    where: { id: { in: variantIds }, isActive: true, isAvailable: true },
                    include: { product: { select: productBasicSelect } }
                })
                : Promise.resolve([]),
            optionIds.length
                ? prisma_1.prisma.modifierOption.findMany({
                    where: { id: { in: optionIds }, isActive: true }
                })
                : Promise.resolve([])
        ]);
        const toProductRef = (prod) => ({
            id: prod.id,
            name: prod.name,
            type: prod.type,
            priceCents: prod.priceCents,
            isActive: prod.isActive,
            isAvailable: prod.isAvailable,
            imageUrl: prod.imageMimeType ? `/products/${prod.id}/image?v=${prod.updatedAt.getTime()}` : null,
            hasImage: Boolean(prod.imageMimeType)
        });
        const toProductBasicRef = (prod) => ({
            id: prod.id,
            name: prod.name,
            type: prod.type,
            priceCents: prod.priceCents,
            isActive: prod.isActive,
            isAvailable: prod.isAvailable,
            imageUrl: prod.imageMimeType ? `/products/${prod.id}/image?v=${prod.updatedAt.getTime()}` : null,
            hasImage: Boolean(prod.imageMimeType)
        });
        const productMap = new Map(products.map((p) => [p.id, toProductRef(p)]));
        const comboMap = new Map(combos.map((combo) => [
            combo.id,
            {
                product: toProductRef(combo),
                components: combo.comboItemsAsCombo.map((component) => ({
                    quantity: component.quantity,
                    isRequired: component.isRequired,
                    notes: component.notes ?? null,
                    product: toProductBasicRef(component.componentProduct),
                    variant: component.componentVariant
                        ? {
                            id: component.componentVariant.id,
                            name: component.componentVariant.name,
                            priceCents: component.componentVariant.priceCents,
                            isActive: component.componentVariant.isActive,
                            isAvailable: component.componentVariant.isAvailable
                        }
                        : null
                }))
            }
        ]));
        const variantMap = new Map(variants.map((v) => [v.id, v]));
        const optionMap = new Map(options.map((o) => [o.id, o]));
        for (const item of items) {
            switch (item.refType) {
                case 'PRODUCT': {
                    const product = productMap.get(item.refId);
                    if (!product)
                        continue;
                    result.set(item.id, {
                        ...item,
                        ref: { kind: 'PRODUCT', product }
                    });
                    break;
                }
                case 'COMBO': {
                    const combo = comboMap.get(item.refId);
                    if (!combo)
                        continue;
                    result.set(item.id, {
                        ...item,
                        ref: { kind: 'COMBO', product: combo.product, components: combo.components }
                    });
                    break;
                }
                case 'VARIANT': {
                    const variant = variantMap.get(item.refId);
                    if (!variant)
                        continue;
                    if (!variant.product.isActive || !variant.product.isAvailable) {
                        continue;
                    }
                    result.set(item.id, {
                        ...item,
                        ref: {
                            kind: 'VARIANT',
                            variant: {
                                id: variant.id,
                                name: variant.name,
                                priceCents: variant.priceCents,
                                isActive: variant.isActive,
                                isAvailable: variant.isAvailable,
                                product: {
                                    ...toProductBasicRef(variant.product)
                                }
                            }
                        }
                    });
                    break;
                }
                case 'OPTION': {
                    const option = optionMap.get(item.refId);
                    if (!option)
                        continue;
                    result.set(item.id, {
                        ...item,
                        ref: {
                            kind: 'OPTION',
                            option: {
                                id: option.id,
                                name: option.name,
                                priceExtraCents: option.priceExtraCents,
                                isActive: option.isActive
                            }
                        }
                    });
                    break;
                }
                default:
                    break;
            }
        }
        return result;
    }
}
exports.PrismaMenuRepository = PrismaMenuRepository;
//# sourceMappingURL=PrismaMenuRepository.js.map