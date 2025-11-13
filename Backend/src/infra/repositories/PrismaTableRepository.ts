import type { ITableRepository } from '../../core/domain/repositories/ITableRepository';
import type { Table } from '@prisma/client';
import { prisma } from '../../lib/prisma';

export class PrismaTableRepository implements ITableRepository {
  create(data: { name: string; seats?: number | null; isActive?: boolean }): Promise<Table> {
    return prisma.table.create({
      data: {
        name: data.name,
        seats: data.seats ?? null,
        isActive: data.isActive ?? true
      }
    });
  }

  list(options?: { includeInactive?: boolean }): Promise<Table[]> {
    const includeInactive = options?.includeInactive ?? false;
    return prisma.table.findMany({
      where: includeInactive ? undefined : { isActive: true },
      orderBy: [{ name: 'asc' }]
    });
  }

  getById(id: number): Promise<Table | null> {
    return prisma.table.findUnique({ where: { id } });
  }

  update(
    id: number,
    data: Partial<Pick<Table, 'name' | 'seats' | 'isActive'>>
  ): Promise<Table> {
    return prisma.table.update({
      where: { id },
      data
    });
  }

  async deactivate(id: number): Promise<void> {
    await prisma.table.update({
      where: { id },
      data: { isActive: false }
    });
  }

  async deleteHard(id: number): Promise<void> {
    await prisma.table.delete({ where: { id } });
  }
}
