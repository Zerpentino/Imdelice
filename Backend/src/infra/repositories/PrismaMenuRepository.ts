import type {
  IMenuRepository,
  MenuItemWithRef,
  MenuPublic
} from '../../core/domain/repositories/IMenuRepository';
import { ProductType } from '@prisma/client';
import type {
  Menu,
  MenuSection,
  MenuItem,
  MenuItemRefType,
  Product,
  ProductVariant,
  ModifierOption
} from '@prisma/client';
import type { Prisma } from '@prisma/client';
import { prisma } from '../../lib/prisma';

type ProductSummaryRow = {
  id: number;
  name: string;
  type: Product['type'];
  priceCents: number | null;
  isActive: boolean;
  isAvailable: boolean;
  imageMimeType: string | null;
  updatedAt: Date;
};

type ProductBasicRow = {
  id: number;
  name: string;
  type: Product['type'];
  priceCents: number | null;
  isActive: boolean;
  isAvailable: boolean;
  imageMimeType: string | null;
  updatedAt: Date;
};

type ComboSummaryRow = ProductSummaryRow & {
  comboItemsAsCombo: Array<{
    quantity: number;
    isRequired: boolean;
    notes: string | null;
    componentProduct: ProductBasicRow;
    componentVariant: {
      id: number;
      name: string;
      priceCents: number;
      isActive: boolean;
      isAvailable: boolean;
    } | null;
  }>;
};

export class PrismaMenuRepository implements IMenuRepository {
  async createMenu(data: {
    name: string;
    isActive?: boolean;
    publishedAt?: Date | null;
    version?: number;
  }): Promise<Menu> {
    return prisma.menu.create({
      data: {
        name: data.name,
        isActive: data.isActive ?? true,
        publishedAt: data.publishedAt ?? null,
        version: data.version ?? 1
      }
    });
  }

  listMenus(): Promise<Menu[]> {
    return prisma.menu.findMany({
      where: { deletedAt: null },
      orderBy: [{ isActive: 'desc' }, { name: 'asc' }]
    });
  }

  listArchivedMenus(): Promise<Menu[]> {
    return prisma.menu.findMany({
      where: { deletedAt: { not: null } },
      orderBy: [{ deletedAt: 'desc' }]
    });
  }

  async updateMenu(
    id: number,
    data: Partial<Pick<Menu, 'name' | 'isActive' | 'publishedAt' | 'version'>>
  ): Promise<Menu> {
    await this.ensureMenuActive(id);
    return prisma.menu.update({ where: { id }, data });
  }

  async deleteMenu(id: number, hard?: boolean): Promise<void> {
    const menu = await prisma.menu.findUnique({ where: { id } });
    if (!menu) throw new Error('Menu not found');

    if (hard) {
      if (menu.publishedAt && !menu.deletedAt) {
        throw new Error('Cannot hard delete a published menu. Unpublish first.');
      }
      await prisma.$transaction(async (tx) => {
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
    await prisma.$transaction([
      prisma.menu.update({
        where: { id },
        data: { isActive: false, deletedAt: now, publishedAt: null }
      }),
      prisma.menuSection.updateMany({
        where: { menuId: id, deletedAt: null },
        data: { isActive: false, deletedAt: now }
      }),
      prisma.menuItem.updateMany({
        where: { section: { menuId: id }, deletedAt: null },
        data: { isActive: false, deletedAt: now }
      })
    ]);
  }

  async restoreMenu(id: number): Promise<void> {
    const archivedMenu = await prisma.menu.findFirst({
      where: { id, deletedAt: { not: null } }
    });
    if (!archivedMenu) {
      throw new Error('Menu is not archived');
    }

    await prisma.$transaction([
      prisma.menu.update({
        where: { id },
        data: { isActive: true, deletedAt: null }
      }),
      prisma.menuSection.updateMany({
        where: { menuId: id, deletedAt: { not: null } },
        data: { isActive: true, deletedAt: null }
      }),
      prisma.menuItem.updateMany({
        where: { section: { menuId: id }, deletedAt: { not: null } },
        data: { isActive: true, deletedAt: null }
      })
    ]);
  }

  async createSection(data: {
    menuId: number;
    name: string;
    position?: number;
    isActive?: boolean;
    categoryId?: number | null;
  }): Promise<MenuSection> {
    await this.ensureMenuActive(data.menuId);
    await this.ensureCategoryExists(data.categoryId);

    return prisma.menuSection.create({
      data: {
        menuId: data.menuId,
        name: data.name,
        position: data.position ?? 0,
        isActive: data.isActive ?? true,
        categoryId: data.categoryId ?? null
      }
    });
  }

  async updateSection(
    id: number,
    data: Partial<Pick<MenuSection, 'name' | 'position' | 'isActive' | 'categoryId'>>
  ): Promise<MenuSection> {
    await this.ensureSectionActive(id);
    if (data.categoryId !== undefined) {
      await this.ensureCategoryExists(data.categoryId);
    }

    const updateData: Prisma.MenuSectionUncheckedUpdateInput = {};
    if (data.name !== undefined) updateData.name = data.name;
    if (data.position !== undefined) updateData.position = data.position;
    if (data.isActive !== undefined) updateData.isActive = data.isActive;
    if (data.categoryId !== undefined) updateData.categoryId = data.categoryId;

    return prisma.menuSection.update({
      where: { id },
      data: updateData
    });
  }

  async deleteSection(id: number, hard?: boolean): Promise<void> {
    const section = await prisma.menuSection.findUnique({ where: { id }, include: { menu: true } });
    if (!section || section.deletedAt) throw new Error('Menu section not found or already deleted');

    if (!section.menu || section.menu.deletedAt) {
      throw new Error('Parent menu is not active');
    }

    if (hard) {
      if (section.menu.publishedAt) {
        throw new Error('Cannot hard delete a section from a published menu. Unpublish first.');
      }
      await prisma.$transaction([
        prisma.menuItem.deleteMany({ where: { sectionId: id } }),
        prisma.menuSection.delete({ where: { id } })
      ]);
      return;
    }

    const now = new Date();
    await prisma.$transaction([
      prisma.menuSection.update({
        where: { id },
        data: { isActive: false, deletedAt: now }
      }),
      prisma.menuItem.updateMany({
        where: { sectionId: id, deletedAt: null },
        data: { isActive: false, deletedAt: now }
      })
    ]);
  }

  async restoreSection(id: number): Promise<void> {
    const section = await prisma.menuSection.findFirst({ where: { id, deletedAt: { not: null } } });
    if (!section) throw new Error('Menu section is not archived');

    await prisma.$transaction([
      prisma.menuSection.update({
        where: { id },
        data: { isActive: true, deletedAt: null }
      }),
      prisma.menuItem.updateMany({
        where: { sectionId: id, deletedAt: { not: null } },
        data: { isActive: true, deletedAt: null }
      })
    ]);
  }

  async deleteSectionHard(id: number): Promise<void> {
    await prisma.$transaction([
      prisma.menuItem.deleteMany({ where: { sectionId: id } }),
      prisma.menuSection.delete({ where: { id } })
    ]);
  }

  listSections(menuId: number): Promise<(MenuSection & { items: MenuItem[] })[]> {
    return prisma.menuSection.findMany({
      where: { menuId, deletedAt: null },
      orderBy: { position: 'asc' },
      include: {
        items: {
          where: { deletedAt: null },
          orderBy: { position: 'asc' }
        }
      }
    }) as Promise<(MenuSection & { items: MenuItem[] })[]>;
  }

  listArchivedSections(menuId: number): Promise<(MenuSection & { items: MenuItem[] })[]> {
    return prisma.menuSection.findMany({
      where: { menuId, deletedAt: { not: null } },
      orderBy: { deletedAt: 'desc' },
      include: {
        items: {
          where: { deletedAt: { not: null } },
          orderBy: { deletedAt: 'desc' }
        }
      }
    }) as Promise<(MenuSection & { items: MenuItem[] })[]>;
  }

  async addItem(data: {
    sectionId: number;
    refType: MenuItemRefType;
    refId: number;
    displayName?: string | null;
    displayPriceCents?: number | null;
    position?: number;
    isFeatured?: boolean;
    isActive?: boolean;
  }): Promise<MenuItem> {
    await this.ensureSectionActive(data.sectionId);
    const ref = await this.ensureRefExists(data.refType, data.refId);

    if (data.refType === 'COMBO' || data.refType === 'PRODUCT') {
      this.assertProductCanBeListed(ref as Product, data.refType);
    }

    return prisma.menuItem.create({
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

  async updateItem(
    id: number,
    data: Partial<
      Pick<MenuItem, 'displayName' | 'displayPriceCents' | 'position' | 'isFeatured' | 'isActive'>
    >
  ): Promise<MenuItem> {
    await this.ensureItemActive(id);
    return prisma.menuItem.update({ where: { id }, data });
  }

  async removeItem(id: number, hard?: boolean): Promise<void> {
    const item = await prisma.menuItem.findUnique({
      where: { id },
      include: { section: { include: { menu: true } } }
    });
    if (!item || item.deletedAt) throw new Error('Menu item not found or already deleted');
    if (!item.section || !item.section.menu || item.section.menu.deletedAt) {
      throw new Error('Parent menu is not active');
    }

    if (hard) {
      if (item.section.menu.publishedAt) {
        throw new Error('Cannot hard delete an item from a published menu. Unpublish first.');
      }
      await prisma.menuItem.delete({ where: { id } });
      return;
    }

    await prisma.menuItem.update({
      where: { id },
      data: { isActive: false, deletedAt: new Date() }
    });
  }

  async restoreItem(id: number): Promise<void> {
    const item = await prisma.menuItem.findFirst({ where: { id, deletedAt: { not: null } } });
    if (!item) throw new Error('Menu item is not archived');

    await prisma.menuItem.update({
      where: { id },
      data: { isActive: true, deletedAt: null }
    });
  }

  async deleteItemHard(id: number): Promise<void> {
    await prisma.menuItem.delete({ where: { id } });
  }

  async listItems(sectionId: number): Promise<MenuItemWithRef[]> {
    const items = await prisma.menuItem.findMany({
      where: { sectionId, deletedAt: null },
      orderBy: { position: 'asc' }
    });
    if (!items.length) return [];
    const resolved = await this.resolveMenuItemRefs(items);
    return items
      .map((item) => resolved.get(item.id))
      .filter((item): item is MenuItemWithRef => Boolean(item));
  }

  async listArchivedItems(sectionId: number): Promise<MenuItemWithRef[]> {
    const items = await prisma.menuItem.findMany({
      where: { sectionId, deletedAt: { not: null } },
      orderBy: { deletedAt: 'desc' }
    });
    if (!items.length) return [];
    const resolved = await this.resolveMenuItemRefs(items);
    return items
      .map((item) => resolved.get(item.id))
      .filter((item): item is MenuItemWithRef => Boolean(item));
  }

  async getMenuPublic(id: number): Promise<MenuPublic | null> {
    const menu = await prisma.menu.findFirst({
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

    if (!menu) return null;

    const allItems = menu.sections.flatMap((section) => section.items);
    const resolved = await this.resolveMenuItemRefs(allItems);

    const sections = menu.sections
      .map((section) => ({
        ...section,
        items: section.items
          .map((item) => resolved.get(item.id))
          .filter((item): item is MenuItemWithRef => Boolean(item))
      }))
      .filter((section) => section.items.length > 0);

    if (!sections.length) return null;

    return { ...menu, sections } as MenuPublic;
  }

  private async ensureMenuActive(id: number): Promise<Menu> {
    const menu = await prisma.menu.findFirst({ where: { id, deletedAt: null } });
    if (!menu) throw new Error('Menu not found or inactive');
    return menu;
  }

  private async ensureSectionActive(id: number): Promise<MenuSection> {
    const section = await prisma.menuSection.findFirst({
      where: { id, deletedAt: null },
      include: { menu: true }
    });
    if (!section || !section.menu || section.menu.deletedAt) {
      throw new Error('Menu section not found or inactive');
    }
    return section;
  }

  private async ensureItemActive(id: number): Promise<MenuItem> {
    const item = await prisma.menuItem.findFirst({
      where: { id, deletedAt: null },
      include: { section: { include: { menu: true } } }
    });
    if (!item || !item.section || !item.section.menu || item.section.menu.deletedAt) {
      throw new Error('Menu item not found or inactive');
    }
    return item;
  }

  private async ensureCategoryExists(categoryId?: number | null): Promise<void> {
    if (!categoryId) return;
    const category = await prisma.category.findUnique({ where: { id: categoryId } });
    if (!category) throw new Error('Category does not exist');
  }

  private assertProductCanBeListed(product: Product, type: MenuItemRefType): void {
    if (type === 'COMBO' && product.type !== ProductType.COMBO) {
      throw new Error('Referenced product is not a combo');
    }
    if (type === 'PRODUCT' && product.type === ProductType.COMBO) {
      throw new Error('Referenced product is a combo, use refType COMBO');
    }
    if (!product.isActive || !product.isAvailable) {
      throw new Error('Referenced product is not active or available');
    }
  }

  private async ensureRefExists(
    refType: MenuItemRefType,
    refId: number
  ): Promise<Product | ProductVariant | ModifierOption> {
    switch (refType) {
      case 'PRODUCT':
      case 'COMBO': {
        const product = await prisma.product.findUnique({ where: { id: refId } });
        if (!product) throw new Error('Referenced product not found');
        if (!product.isActive || !product.isAvailable) {
          throw new Error('Referenced product is not active or available');
        }
        return product;
      }
      case 'VARIANT': {
        const variant = await prisma.productVariant.findUnique({
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
        const option = await prisma.modifierOption.findUnique({ where: { id: refId } });
        if (!option) throw new Error('Referenced modifier option not found');
        if (!option.isActive) throw new Error('Modifier option is not active');
        return option;
      }
      default:
        throw new Error(`Unsupported refType: ${refType}`);
    }
  }

  private async resolveMenuItemRefs(items: MenuItem[]): Promise<Map<number, MenuItemWithRef>> {
    const grouped = new Map<MenuItemRefType, number[]>();
    for (const item of items) {
      if (!grouped.has(item.refType)) grouped.set(item.refType, []);
      grouped.get(item.refType)?.push(item.refId);
    }

    const result = new Map<number, MenuItemWithRef>();

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
    } as const;

    const productBasicSelect = {
      id: true,
      name: true,
      type: true,
      priceCents: true,
      isActive: true,
      isAvailable: true,
      imageMimeType: true,
      updatedAt: true
    } as const;

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
    } as const;

    const [products, combos, variants, options] = await Promise.all([
      productIds.length
        ? prisma.product.findMany({
            where: {
              id: { in: productIds },
              isActive: true,
              isAvailable: true,
              type: { not: ProductType.COMBO }
            },
            select: productSelect
          }) as Promise<ProductSummaryRow[]>
        : Promise.resolve([] as ProductSummaryRow[]),
      comboIds.length
        ? prisma.product.findMany({
            where: {
              id: { in: comboIds },
              isActive: true,
              isAvailable: true,
              type: ProductType.COMBO
            },
            select: comboSelect
          }) as Promise<ComboSummaryRow[]>
        : Promise.resolve([] as ComboSummaryRow[]),
      variantIds.length
        ? prisma.productVariant.findMany({
            where: { id: { in: variantIds }, isActive: true, isAvailable: true },
            include: { product: { select: productBasicSelect } }
          }) as Promise<Array<ProductVariant & { product: ProductBasicRow }>>
        : Promise.resolve([] as Array<ProductVariant & { product: ProductBasicRow }>),
      optionIds.length
        ? prisma.modifierOption.findMany({
            where: { id: { in: optionIds }, isActive: true }
          })
        : Promise.resolve([] as ModifierOption[])
    ]);

    const toProductRef = (prod: ProductSummaryRow) => ({
      id: prod.id,
      name: prod.name,
      type: prod.type,
      priceCents: prod.priceCents,
      isActive: prod.isActive,
      isAvailable: prod.isAvailable,
      imageUrl: prod.imageMimeType ? `/products/${prod.id}/image?v=${prod.updatedAt.getTime()}` : null,
      hasImage: Boolean(prod.imageMimeType)
    });

    const toProductBasicRef = (prod: ProductBasicRow) => ({
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
    const comboMap = new Map(
      combos.map((combo) => [
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
      ])
    );
    const variantMap = new Map(variants.map((v) => [v.id, v]));
    const optionMap = new Map(options.map((o) => [o.id, o]));

    for (const item of items) {
      switch (item.refType) {
        case 'PRODUCT': {
          const product = productMap.get(item.refId);
          if (!product) continue;
          result.set(item.id, {
            ...item,
            ref: { kind: 'PRODUCT', product }
          });
          break;
        }
        case 'COMBO': {
          const combo = comboMap.get(item.refId);
          if (!combo) continue;
          result.set(item.id, {
            ...item,
            ref: { kind: 'COMBO', product: combo.product, components: combo.components }
          });
          break;
        }
        case 'VARIANT': {
          const variant = variantMap.get(item.refId);
          if (!variant) continue;
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
          if (!option) continue;
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
