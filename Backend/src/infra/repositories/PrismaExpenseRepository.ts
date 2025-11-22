import { Prisma } from "@prisma/client";
import { prisma } from "../../lib/prisma";
import type {
  CreateExpenseInput,
  ExpenseFilters,
  ExpenseSummaryResult,
  IExpenseRepository,
  UpdateExpenseInput,
} from "../../core/domain/repositories/IExpenseRepository";

export class PrismaExpenseRepository implements IExpenseRepository {
  create(data: CreateExpenseInput) {
    return prisma.expense.create({
      data: {
        concept: data.concept,
        category: data.category,
        amountCents: data.amountCents,
        paymentMethod: data.paymentMethod ?? null,
        notes: data.notes ?? null,
        incurredAt: data.incurredAt ?? new Date(),
        recordedByUserId: data.recordedByUserId,
        inventoryMovementId: data.inventoryMovementId ?? null,
        metadata: data.metadata ?? Prisma.JsonNull,
      },
    });
  }

  list(filters: ExpenseFilters = {}) {
    return prisma.expense.findMany({
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

  getById(id: number) {
    return prisma.expense.findUnique({
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

  update(id: number, data: UpdateExpenseInput) {
    return prisma.expense.update({
      where: { id },
      data: {
        concept: data.concept,
        category: data.category,
        amountCents: data.amountCents,
        paymentMethod: data.paymentMethod,
        notes: data.notes,
        incurredAt: data.incurredAt,
        inventoryMovementId:
          data.inventoryMovementId === undefined
            ? undefined
            : data.inventoryMovementId,
        metadata: data.metadata ?? undefined,
      },
    });
  }

  async delete(id: number): Promise<void> {
    await prisma.expense.delete({ where: { id } });
  }

  async getSummary(filters: ExpenseFilters = {}): Promise<ExpenseSummaryResult> {
    const expenses = await prisma.expense.groupBy({
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
    const totalAmountCents = totalsByCategory.reduce(
      (acc, curr) => acc + curr.amountCents,
      0,
    );

    return { totalAmountCents, totalsByCategory };
  }
}
