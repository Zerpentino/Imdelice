"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.PrismaExpenseRepository = void 0;
const client_1 = require("@prisma/client");
const prisma_1 = require("../../lib/prisma");
class PrismaExpenseRepository {
    create(data) {
        return prisma_1.prisma.expense.create({
            data: {
                concept: data.concept,
                category: data.category,
                amountCents: data.amountCents,
                paymentMethod: data.paymentMethod ?? null,
                notes: data.notes ?? null,
                incurredAt: data.incurredAt ?? new Date(),
                recordedByUserId: data.recordedByUserId,
                inventoryMovementId: data.inventoryMovementId ?? null,
                metadata: data.metadata ?? client_1.Prisma.JsonNull,
            },
        });
    }
    list(filters = {}) {
        return prisma_1.prisma.expense.findMany({
            where: {
                category: filters.category,
                recordedByUserId: filters.recordedByUserId,
                incurredAt: {
                    gte: filters.from,
                    lte: filters.to,
                },
                concept: filters.search
                    ? { contains: filters.search }
                    : undefined,
            },
            orderBy: [{ incurredAt: "desc" }, { id: "desc" }],
            include: {
                recordedBy: { select: { id: true, name: true, email: true } },
                inventoryMovement: {
                    include: {
                        product: { select: { id: true, name: true, sku: true, barcode: true } },
                        variant: { select: { id: true, name: true } },
                        location: { select: { id: true, name: true } },
                    },
                },
            },
        });
    }
    getById(id) {
        return prisma_1.prisma.expense.findUnique({
            where: { id },
            include: {
                recordedBy: { select: { id: true, name: true, email: true } },
                inventoryMovement: {
                    include: {
                        product: { select: { id: true, name: true, sku: true, barcode: true } },
                        variant: { select: { id: true, name: true } },
                        location: { select: { id: true, name: true } },
                    },
                },
            },
        });
    }
    update(id, data) {
        return prisma_1.prisma.expense.update({
            where: { id },
            data: {
                concept: data.concept,
                category: data.category,
                amountCents: data.amountCents,
                paymentMethod: data.paymentMethod,
                notes: data.notes,
                incurredAt: data.incurredAt,
                inventoryMovementId: data.inventoryMovementId === undefined
                    ? undefined
                    : data.inventoryMovementId,
                metadata: data.metadata ?? undefined,
            },
        });
    }
    async delete(id) {
        await prisma_1.prisma.expense.delete({ where: { id } });
    }
    async getSummary(filters = {}) {
        const expenses = await prisma_1.prisma.expense.groupBy({
            by: ["category"],
            where: {
                category: filters.category,
                recordedByUserId: filters.recordedByUserId,
                incurredAt: {
                    gte: filters.from,
                    lte: filters.to,
                },
            },
            _sum: { amountCents: true },
        });
        const totalsByCategory = expenses.map((item) => ({
            category: item.category,
            amountCents: item._sum.amountCents || 0,
        }));
        const totalAmountCents = totalsByCategory.reduce((acc, curr) => acc + curr.amountCents, 0);
        return { totalAmountCents, totalsByCategory };
    }
}
exports.PrismaExpenseRepository = PrismaExpenseRepository;
//# sourceMappingURL=PrismaExpenseRepository.js.map