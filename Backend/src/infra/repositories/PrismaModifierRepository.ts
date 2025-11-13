import type { IModifierRepository } from '../../core/domain/repositories/IModifierRepository';
import type { ModifierGroup, ModifierOption } from '@prisma/client';
import { prisma } from '../../lib/prisma';
import type { Product } from '@prisma/client';

export class PrismaModifierRepository implements IModifierRepository {
  async listGroups(filter?: {
    isActive?: boolean; categoryId?: number; categorySlug?: string; search?: string;
  }): Promise<(ModifierGroup & { options: ModifierOption[] })[]> {
    return prisma.modifierGroup.findMany({
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
  getGroup(id: number): Promise<(ModifierGroup & { options: ModifierOption[] }) | null> {
    return prisma.modifierGroup.findUnique({
      where: { id },
      include: { options: true }, // 👈 igual aquí
    });
  }

  async listByProduct(productId: number): Promise<
    { id: number; position: number; group: ModifierGroup & { options: ModifierOption[] } }[]
  > {
    const links = await prisma.productModifierGroup.findMany({
      where: { productId },
      include: { group: { include: { options: true } } }, // 👈 incluir options
      orderBy: { position: 'asc' },
    });
    return links.map(l => ({ id: l.id, position: l.position, group: l.group }));
  }



  async createGroupWithOptions(data: { name: string; description?: string; minSelect?: number; maxSelect?: number | null; isRequired?: boolean;appliesToCategoryId?: number | null;
    options: { name: string; priceExtraCents?: number; isDefault?: boolean; position?: number }[]; }): Promise<{ group: ModifierGroup; options: ModifierOption[] }> {
    const group = await prisma.modifierGroup.create({
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

  updateGroup(id: number, data: Partial<Pick<ModifierGroup,'name'|'description'|'minSelect'|'maxSelect'|'isRequired'|'isActive'|'appliesToCategoryId'>>): Promise<ModifierGroup> {
    return prisma.modifierGroup.update({ where: { id }, data });
  }

async replaceOptions(
  groupId: number,
  options: { id?: number; name: string; priceExtraCents?: number; isDefault?: boolean; position?: number }[]
): Promise<void> {
  // 1) Traer las actuales
  const current = await prisma.modifierOption.findMany({ where: { groupId } });
  const currentByName = new Map(current.map(o => [o.name, o]));
  const payloadByName = new Map(options.map(o => [o.name, o]));

  // 2) Preparar operaciones
  const toUpdate = [];
  const toCreate = [];
  const candidatesToDelete: number[] = [];

  // a) Actualizar las que coinciden por nombre
  for (const o of options) {
    const existing = currentByName.get(o.name);
    if (existing) {
      toUpdate.push(
        prisma.modifierOption.update({
          where: { id: existing.id },
          data: {
            priceExtraCents: o.priceExtraCents ?? 0,
            isDefault: !!o.isDefault,
            position: o.position ?? 0,
            isActive: true,
          },
        })
      );
    } else {
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
  const used = await prisma.orderItemModifier.findMany({
    where: { optionId: { in: candidatesToDelete } },
    select: { optionId: true },
  });
  const usedSet = new Set(used.map(u => u.optionId));

  const toSoftDisable = candidatesToDelete.filter(id => usedSet.has(id));
  const toHardDelete  = candidatesToDelete.filter(id => !usedSet.has(id));

  // 4) Ejecutar en transacción
  await prisma.$transaction([
    // actualizar
    ...toUpdate,
    // crear nuevas
    prisma.modifierOption.createMany({ data: toCreate }),
    // desactivar las con historial
    ...(toSoftDisable.length
      ? [prisma.modifierOption.updateMany({ where: { id: { in: toSoftDisable } }, data: { isActive: false } })]
      : []),
    // borrar las sin uso
    ...(toHardDelete.length
      ? [prisma.modifierOption.deleteMany({ where: { id: { in: toHardDelete } } })]
      : []),
  ]);
}
async updateOption(
  id: number,
  data: Partial<Pick<ModifierOption,'name'|'priceExtraCents'|'isDefault'|'position'|'isActive'>>
): Promise<ModifierOption> {
  return prisma.modifierOption.update({ where: { id }, data });
}

  

  async deactivateGroup(id: number): Promise<void> {
    await prisma.modifierGroup.update({ where: { id }, data: { isActive: false } });
  }
// src/infra/repositories/PrismaModifierRepository.ts
async deleteGroupHard(id: number): Promise<void> {
  await prisma.$transaction(async (tx) => {
    // 1) Links a productos
    await tx.productModifierGroup.deleteMany({ where: { groupId: id } });

    // 2) Opciones del grupo
    await tx.modifierOption.deleteMany({ where: { groupId: id } });

    // 3) Borra el grupo
    await tx.modifierGroup.delete({ where: { id } });
  });
}

async listProductsByGroup(
  groupId: number,
  filter?: { isActive?: boolean; search?: string; limit?: number; offset?: number }
): Promise<Array<{ linkId: number; position: number; product: Pick<Product,'id'|'name'|'type'|'priceCents'|'isActive'|'categoryId'> }>> {
  const links = await prisma.productModifierGroup.findMany({
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
