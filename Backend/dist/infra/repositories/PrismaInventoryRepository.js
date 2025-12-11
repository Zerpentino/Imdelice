"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.PrismaInventoryRepository = void 0;
const client_1 = require("@prisma/client");
const prisma_1 = require("../../lib/prisma");
class PrismaInventoryRepository {
    async getDefaultLocation() {
        return prisma_1.prisma.inventoryLocation.findFirst({
            where: { isDefault: true },
            orderBy: { id: "asc" },
        });
    }
    async ensureDefaultLocation() {
        const existing = await this.getDefaultLocation();
        if (existing)
            return existing;
        return prisma_1.prisma.inventoryLocation.create({
            data: {
                name: "General",
                type: "GENERAL",
                isDefault: true,
            },
        });
    }
    async findItem(filter) {
        return prisma_1.prisma.inventoryItem.findFirst({
            where: {
                id: filter.id,
                productId: filter.productId,
                locationId: filter.locationId,
                variantId: filter.variantId === undefined ? undefined : filter.variantId ?? null,
            },
            include: {
                product: {
                    select: {
                        id: true,
                        name: true,
                        description: true,
                        type: true,
                        categoryId: true,
                        priceCents: true,
                        isActive: true,
                        sku: true,
                        barcode: true,
                        imageMimeType: true,
                        imageSize: true,
                        isAvailable: true,
                        createdAt: true,
                        updatedAt: true,
                        category: { select: { id: true, name: true, slug: true } },
                    },
                },
                variant: true,
                location: true,
            },
        }).then(item => (item ? this.attachProductImageMeta(item) : null));
    }
    async listItems(filter = {}) {
        const items = await prisma_1.prisma.inventoryItem.findMany({
            where: {
                id: filter.id,
                productId: filter.productId,
                locationId: filter.locationId,
                variantId: filter.variantId === undefined ? undefined : filter.variantId ?? null,
                product: filter.categoryId || filter.search
                    ? {
                        is: {
                            categoryId: filter.categoryId,
                            name: filter.search
                                ? {
                                    contains: filter.search,
                                }
                                : undefined,
                        },
                    }
                    : undefined,
            },
            include: {
                product: {
                    select: {
                        id: true,
                        name: true,
                        description: true,
                        type: true,
                        categoryId: true,
                        priceCents: true,
                        isActive: true,
                        sku: true,
                        barcode: true,
                        imageMimeType: true,
                        imageSize: true,
                        isAvailable: true,
                        createdAt: true,
                        updatedAt: true,
                        category: { select: { id: true, name: true, slug: true } },
                    },
                },
                variant: true,
                location: true,
            },
            orderBy: { updatedAt: "desc" },
        });
        const itemsWithMeta = items.map(item => this.attachProductImageMeta(item));
        const defaultLocation = await prisma_1.prisma.inventoryLocation.findFirst({
            where: { isDefault: true },
            select: {
                id: true,
                name: true,
                code: true,
                type: true,
                isDefault: true,
                isActive: true,
                createdAt: true,
                updatedAt: true,
            },
        });
        const inventoryProducts = await prisma_1.prisma.product.findMany({
            where: { category: { slug: "inventario" } },
            select: {
                id: true,
                name: true,
                description: true,
                type: true,
                categoryId: true,
                priceCents: true,
                isActive: true,
                sku: true,
                barcode: true,
                imageMimeType: true,
                imageSize: true,
                isAvailable: true,
                createdAt: true,
                updatedAt: true,
                category: { select: { id: true, name: true, slug: true } },
            },
        });
        const existingIds = new Set(items.map(it => it.productId));
        const filterLocationId = filter.locationId ?? null;
        for (const product of inventoryProducts) {
            if (existingIds.has(product.id))
                continue;
            if (filterLocationId && defaultLocation?.id !== filterLocationId)
                continue;
            const placeholder = {
                id: -product.id,
                productId: product.id,
                variantId: null,
                locationId: defaultLocation?.id ?? 0,
                currentQuantity: new client_1.Prisma.Decimal(0),
                unit: "UNIT",
                minThreshold: null,
                maxThreshold: null,
                lastMovementAt: null,
                createdAt: product.createdAt,
                updatedAt: product.updatedAt,
                product,
                variant: null,
                location: defaultLocation ?? null,
            };
            itemsWithMeta.push(this.attachProductImageMeta(placeholder));
        }
        return itemsWithMeta;
    }
    async listMovements(filter = {}) {
        return prisma_1.prisma.inventoryMovement.findMany({
            where: {
                productId: filter.productId,
                locationId: filter.locationId,
                variantId: filter.variantId === undefined ? undefined : filter.variantId ?? null,
                relatedOrderId: filter.orderId,
                type: filter.type,
            },
            include: {
                product: { select: { id: true, name: true, sku: true, barcode: true } },
                variant: { select: { id: true, name: true, sku: true, barcode: true } },
                location: true,
            },
            orderBy: { createdAt: "desc" },
            take: filter.limit,
        });
    }
    async hasOrderMovement(orderId, type) {
        const count = await prisma_1.prisma.inventoryMovement.count({
            where: { relatedOrderId: orderId, type },
        });
        return count > 0;
    }
    async createMovement(input) {
        if (!input.quantity || Number.isNaN(input.quantity)) {
            throw new Error("Quantity must be a non-zero number");
        }
        return prisma_1.prisma.$transaction(async (tx) => {
            const locationId = await this.resolveLocationId(tx, input.locationId);
            const quantity = new client_1.Prisma.Decimal(input.quantity);
            const unit = input.unit ?? client_1.UnitOfMeasure.UNIT;
            const movement = await tx.inventoryMovement.create({
                data: {
                    productId: input.productId,
                    variantId: input.variantId ?? null,
                    locationId,
                    type: input.type,
                    quantity,
                    unit,
                    reason: input.reason ?? null,
                    relatedOrderId: input.relatedOrderId ?? null,
                    performedByUserId: input.performedByUserId ?? null,
                    metadata: input.metadata ?? client_1.Prisma.JsonNull,
                },
            });
            const existingItem = await tx.inventoryItem.findFirst({
                where: {
                    productId: input.productId,
                    variantId: input.variantId ?? null,
                    locationId,
                },
                select: { id: true },
            });
            if (existingItem) {
                await tx.inventoryItem.update({
                    where: { id: existingItem.id },
                    data: {
                        currentQuantity: { increment: quantity },
                        unit,
                        lastMovementAt: movement.createdAt,
                    },
                });
            }
            else {
                await tx.inventoryItem.create({
                    data: {
                        productId: input.productId,
                        variantId: input.variantId ?? null,
                        locationId,
                        currentQuantity: quantity,
                        unit,
                        lastMovementAt: movement.createdAt,
                    },
                });
            }
            return movement;
        });
    }
    async resolveLocationId(tx, locationId) {
        if (locationId) {
            const exists = await tx.inventoryLocation.findUnique({
                where: { id: locationId },
                select: { id: true },
            });
            if (!exists)
                throw new Error("Inventory location not found");
            return locationId;
        }
        const existing = await tx.inventoryLocation.findFirst({
            where: { isDefault: true },
            orderBy: { id: "asc" },
            select: { id: true },
        });
        if (existing)
            return existing.id;
        const created = await tx.inventoryLocation.create({
            data: { name: "General", type: "GENERAL", isDefault: true },
        });
        return created.id;
    }
    async listLocations() {
        return prisma_1.prisma.inventoryLocation.findMany({
            orderBy: [{ isDefault: "desc" }, { name: "asc" }],
        });
    }
    async createLocation(data) {
        return prisma_1.prisma.$transaction(async (tx) => {
            if (data.isDefault) {
                await tx.inventoryLocation.updateMany({
                    data: { isDefault: false },
                    where: { isDefault: true },
                });
            }
            return tx.inventoryLocation.create({
                data: {
                    name: data.name,
                    code: data.code ?? null,
                    type: data.type ?? "GENERAL",
                    isDefault: data.isDefault ?? false,
                },
            });
        });
    }
    async updateLocation(id, data) {
        return prisma_1.prisma.$transaction(async (tx) => {
            if (data.isDefault) {
                await tx.inventoryLocation.updateMany({
                    data: { isDefault: false },
                    where: { isDefault: true },
                });
            }
            return tx.inventoryLocation.update({
                where: { id },
                data: {
                    name: data.name,
                    code: data.code === undefined ? undefined : data.code,
                    type: data.type,
                    isDefault: data.isDefault ?? undefined,
                    isActive: data.isActive ?? undefined,
                },
            });
        });
    }
    async deleteLocation(id) {
        await prisma_1.prisma.$transaction(async (tx) => {
            const location = await tx.inventoryLocation.findUnique({ where: { id } });
            if (!location)
                throw new Error("Inventory location not found");
            if (location.isDefault) {
                throw new Error("Cannot delete default location");
            }
            const hasItems = await tx.inventoryItem.count({
                where: { locationId: id },
            });
            if (hasItems) {
                throw new Error("Location in use by inventory items");
            }
            await tx.inventoryLocation.delete({ where: { id } });
        });
    }
    attachProductImageMeta(item) {
        if (!item.product)
            return item;
        const product = item.product;
        const imageUrl = product.imageMimeType
            ? `/products/${product.id}/image?v=${product.updatedAt.getTime()}`
            : null;
        return {
            ...item,
            product: {
                ...product,
                imageUrl,
                hasImage: Boolean(product.imageMimeType),
                categoryName: product.category?.name ?? null,
            },
        };
    }
}
exports.PrismaInventoryRepository = PrismaInventoryRepository;
//# sourceMappingURL=PrismaInventoryRepository.js.map