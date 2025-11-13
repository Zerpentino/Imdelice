import type { IProductRepository, ProductListItem, ProductWithDetails } from '../../core/domain/repositories/IProductRepository';
import type { Product } from '@prisma/client';
import { Prisma } from '@prisma/client';
import { prisma } from '../../lib/prisma';

export class PrismaProductRepository implements IProductRepository {
    createSimple(data: {
    name: string; categoryId: number; priceCents: number;
    description?: string; sku?: string;
    image?: { buffer: Buffer; mimeType: string; size: number };
  }): Promise<Product> {
    return prisma.product.create({
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

 createVarianted(data: {
    name: string; categoryId: number;
    variants: { name: string; priceCents: number; sku?: string }[];
    description?: string; sku?: string;
    image?: { buffer: Buffer; mimeType: string; size: number };
  }): Promise<Product> {
    return prisma.product.create({
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
update(
  id: number,
  data: Partial<Pick<Product, 'name'|'categoryId'|'priceCents'|'description'|'sku'|'isActive'>> & {
    image?: { buffer: Buffer; mimeType: string; size: number } | null;
  }
): Promise<Product> {
  return prisma.$transaction(async (tx) => {
    // 1) Saber qué tipo es el producto actual
    const current = await tx.product.findUnique({
      where: { id },
      select: { type: true }
    });
    if (!current) throw new Error('Product not found');

    // 2) Si el producto es COMBO y quieren cambiar la categoría, validar isComboOnly
    if (current.type === 'COMBO' && data.categoryId !== undefined) {
      const cat = await tx.category.findUnique({ where: { id: data.categoryId } });
      if (!cat) throw new Error('Category not found');
      if (cat.isComboOnly !== true) throw new Error('This category does not accept combos');
    }

    // 3) Actualización normal + manejo de imagen (undefined = no tocar; null = borrar; objeto = reemplazar)
    const updated = await tx.product.update({
      where: { id },
      data: {
        ...data,
        image:         data.image === undefined ? undefined : (data.image ? data.image.buffer : null),
        imageMimeType: data.image === undefined ? undefined : (data.image ? data.image.mimeType : null),
        imageSize:     data.image === undefined ? undefined : (data.image ? data.image.size : null),
      }
    });

    return updated;
  });
}



  
  async replaceVariants(productId: number, variants: { name: string; priceCents: number; sku?: string }[]): Promise<void> {
    await prisma.$transaction([
      prisma.productVariant.deleteMany({ where: { productId } }),
      prisma.product.update({
        where: { id: productId },
        data: { variants: { create: variants.map(v => ({ name: v.name, priceCents: v.priceCents, sku: v.sku })) } }
      })
    ]);
  }
   // NUEVO: conversiones
  async convertToVarianted(productId: number, variants: { name: string; priceCents: number; sku?: string }[]): Promise<void> {
    await prisma.$transaction([
      prisma.product.update({ where: { id: productId }, data: { type: 'VARIANTED', priceCents: null } }),
      prisma.productVariant.deleteMany({ where: { productId } }),
      prisma.product.update({
        where: { id: productId },
        data: { variants: { create: variants.map(v => ({ name: v.name, priceCents: v.priceCents, sku: v.sku })) } }
      })
    ]);
  }
   async convertToSimple(productId: number, priceCents: number): Promise<void> {
    await prisma.$transaction([
      prisma.productVariant.deleteMany({ where: { productId } }),
      prisma.product.update({ where: { id: productId }, data: { type: 'SIMPLE', priceCents } })
    ]);
  }


  getById(id: number): Promise<ProductWithDetails | null> {
    return prisma.product.findUnique({
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
    }) as any;
  }


  async list(filter?: { categorySlug?: string; type?: 'SIMPLE'|'VARIANTED'|'COMBO'; isActive?: boolean }): Promise<ProductListItem[]> {
    const products = await prisma.product.findMany({
      where: {
        type: filter?.type as any,
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

  async attachModifierGroup(productId: number, groupId: number, position = 0): Promise<void> {
    await prisma.productModifierGroup.create({ data: { productId, groupId, position } });
  }

  async attachModifierGroupToVariant(
    variantId: number,
    data: { groupId: number; minSelect?: number; maxSelect?: number | null; isRequired?: boolean }
  ): Promise<void> {
    const createPayload = {
      variantId,
      groupId: data.groupId,
      minSelect: data.minSelect ?? 0,
      maxSelect: data.maxSelect ?? null,
      isRequired: data.isRequired ?? false
    };

    try {
      await prisma.productVariantModifierGroup.create({ data: createPayload });
    } catch (err: any) {
      if (err instanceof Prisma.PrismaClientKnownRequestError && err.code === 'P2002') {
        await this.updateVariantModifierGroup(variantId, data.groupId, data);
      } else {
        throw err;
      }
    }
  }

  async updateVariantModifierGroup(
    variantId: number,
    groupId: number,
    data: { minSelect?: number; maxSelect?: number | null; isRequired?: boolean }
  ): Promise<void> {
    const updateData: Prisma.ProductVariantModifierGroupUpdateInput = {};
    if (data.minSelect !== undefined) updateData.minSelect = data.minSelect;
    if (data.maxSelect !== undefined) updateData.maxSelect = data.maxSelect;
    if (data.isRequired !== undefined) updateData.isRequired = data.isRequired;

    if (!Object.keys(updateData).length) return;

    await prisma.productVariantModifierGroup.update({
      where: { variantId_groupId: { variantId, groupId } },
      data: updateData
    });
  }

  async detachModifierGroupFromVariant(variantId: number, groupId: number): Promise<void> {
    await prisma.productVariantModifierGroup.deleteMany({
      where: { variantId, groupId }
    });
  }

  async listVariantModifierGroups(variantId: number) {
    const links = await prisma.productVariantModifierGroup.findMany({
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

  async deactivate(id: number): Promise<void> {
    await prisma.product.update({ where: { id }, data: { isActive: false } });
  }

  async deleteHard(id: number): Promise<void> {
    await prisma.$transaction(async (tx) => {
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
      const menuItemFilters: any[] = [
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

async createCombo(data: {
  name: string; categoryId: number; priceCents: number; description?: string; sku?: string;
  image?: { buffer: Buffer; mimeType: string; size: number };
  items?: {
    componentProductId: number;
    componentVariantId?: number;
    quantity?: number;
    isRequired?: boolean;
    notes?: string;
  }[];
}): Promise<Product> {
    const cat = await prisma.category.findUnique({ where: { id: data.categoryId } })
  if (!cat) throw new Error('Category not found')
  if (cat.isComboOnly !== true) throw new Error('This category does not accept combos')

    const items = data.items ?? [];
    if (items.length) {
      const variantIds = items
        .map(it => it.componentVariantId)
        .filter((id): id is number => typeof id === 'number');
      if (variantIds.length) {
        const variants = await prisma.productVariant.findMany({
          where: { id: { in: variantIds } },
          select: { id: true, productId: true }
        });
        const variantMap = new Map(variants.map(v => [v.id, v.productId]));
        for (const item of items) {
          if (item.componentVariantId !== undefined) {
            const ownerProductId = variantMap.get(item.componentVariantId);
            if (!ownerProductId) throw new Error('Component variant not found');
            if (ownerProductId !== item.componentProductId) {
              throw new Error('Component variant does not belong to provided product');
            }
          }
        }
      }
    }

    return prisma.product.create({
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

  async addComboItems(comboProductId: number, items: {
    componentProductId: number;
    componentVariantId?: number;
    quantity?: number;
    isRequired?: boolean;
    notes?: string;
  }[]): Promise<void> {
    if (!items.length) return;
    const variantIds = items
      .map(it => it.componentVariantId)
      .filter((id): id is number => typeof id === 'number');
    if (variantIds.length) {
      const variants = await prisma.productVariant.findMany({
        where: { id: { in: variantIds } },
        select: { id: true, productId: true }
      });
      const variantMap = new Map(variants.map(v => [v.id, v.productId]));
      for (const item of items) {
        if (item.componentVariantId !== undefined) {
          const ownerProductId = variantMap.get(item.componentVariantId);
          if (!ownerProductId) throw new Error('Component variant not found');
          if (ownerProductId !== item.componentProductId) {
            throw new Error('Component variant does not belong to provided product');
          }
        }
      }
    }
    await prisma.comboItem.createMany({
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

  async updateComboItem(comboItemId: number, data: Partial<{ quantity: number; isRequired: boolean; notes: string }>): 
  Promise<void> {


    await prisma.comboItem.update({
      where: { id: comboItemId },
      data: {
        quantity: data.quantity,
        isRequired: data.isRequired,
        notes: data.notes ?? null
      }
    });
  }

  async removeComboItem(comboItemId: number): Promise<void> {
    await prisma.comboItem.delete({ where: { id: comboItemId } });
  }


  // Quitar vínculo por linkId o por (productId, groupId)
async detachModifierGroup(linkIdOr: { linkId: number } | { productId: number; groupId: number }): Promise<void> {
  if ('linkId' in linkIdOr) {
    await prisma.productModifierGroup.delete({ where: { id: linkIdOr.linkId } });
  } else {
    await prisma.productModifierGroup.deleteMany({
      where: { productId: linkIdOr.productId, groupId: linkIdOr.groupId }
    });
  }
}

async updateModifierGroupPosition(linkId: number, position: number): Promise<void> {
  await prisma.productModifierGroup.update({
    where: { id: linkId },
    data: { position }
  });
}

async reorderModifierGroups(
  productId: number,
  items: Array<{ linkId: number; position: number }>
): Promise<void> {
  await prisma.$transaction(
    items.map(i =>
      prisma.productModifierGroup.update({
        where: { id: i.linkId },
        data: { position: i.position }
      })
    )
  );
}
// --- DISPONIBILIDAD (PRODUCTO y VARIANTE) ---

async setProductAvailability(productId: number, isAvailable: boolean): Promise<void> {
  await prisma.product.update({
    where: { id: productId },
    data: { isAvailable }
  });
}

async setVariantAvailability(
  variantId: number,
  isAvailable: boolean,
  expectProductId?: number
): Promise<void> {
  // Validación opcional para asegurar pertenencia
  if (expectProductId !== undefined) {
    const variant = await prisma.productVariant.findUnique({
      where: { id: variantId },
      select: { id: true, productId: true }
    });
    if (!variant) throw new Error('Variant not found');
    if (variant.productId !== expectProductId) {
      throw new Error('Variant does not belong to the expected product');
    }
  }

  await prisma.productVariant.update({
    where: { id: variantId },
    data: { isAvailable }
  });
}

}
