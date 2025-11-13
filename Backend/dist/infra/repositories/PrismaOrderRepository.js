"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.PrismaOrderRepository = void 0;
// src/infra/repositories/PrismaOrderRepository.ts
const prisma_1 = require("../../lib/prisma");
const MONTH_NAMES = [
    "enero",
    "febrero",
    "marzo",
    "abril",
    "mayo",
    "junio",
    "julio",
    "agosto",
    "septiembre",
    "octubre",
    "noviembre",
    "diciembre",
];
function formatNoteCode(noteNumber, when, attendedBy) {
    const day = when.getDate();
    const monthName = MONTH_NAMES[when.getMonth()];
    const year = when.getFullYear();
    let suffix = "";
    if (attendedBy?.id ?? attendedBy?.name) {
        const namePart = attendedBy?.name?.trim() || null;
        const idPart = attendedBy?.id != null ? `ID ${attendedBy.id}` : null;
        const descriptor = [namePart, idPart].filter(Boolean).join(" - ");
        suffix = descriptor ? ` - Atendido por: ${descriptor}` : "";
    }
    return `NOTA ${noteNumber} del ${day} de ${monthName} de ${year}${suffix}`;
}
class PrismaOrderRepository {
    async create(input, servedByUserId) {
        return prisma_1.prisma.$transaction(async (tx) => {
            const status = (input.status ?? "OPEN");
            const now = new Date();
            const markupPct = input.platformMarkupPct !== undefined
                ? input.platformMarkupPct
                : await this.getMarkupForSource(tx, input.source ?? "POS");
            const code = await this.generateSequentialCode(tx, servedByUserId);
            const data = {
                code,
                status: status,
                serviceType: input.serviceType,
                source: (input.source ?? "POS"),
                externalRef: input.externalRef ?? null,
                prepEtaMinutes: input.prepEtaMinutes ?? null,
                tableId: input.tableId ?? null,
                note: input.note ?? null,
                covers: input.covers ?? null,
                servedByUserId,
                platformMarkupPct: markupPct ?? null,
            };
            if (status === "OPEN") {
                data.acceptedAt = now;
            }
            return tx.order.create({ data });
        });
    }
    // El tipo de retorno depende de tu interfaz; si la interfaz espera Order | null, puedes tiparlo así:
    // getById(orderId: number): Promise<Order | null> { ... }
    // Aquí dejamos que TS infiera; si quieres lo tipamos a Promise<any>.
    getById(orderId) {
        return prisma_1.prisma.order.findUnique({
            where: { id: orderId },
            include: {
                items: {
                    include: {
                        variant: true,
                        product: { select: { id: true, name: true, type: true } },
                        modifiers: true,
                        childItems: {
                            include: {
                                variant: true,
                                product: { select: { id: true, name: true, type: true } },
                                modifiers: true,
                                parentItem: { select: { id: true } },
                            },
                        },
                        parentItem: { select: { id: true } },
                    },
                    orderBy: { id: "asc" },
                },
                payments: true,
                table: true,
                servedBy: { select: { id: true, name: true, email: true } },
            },
        });
    }
    async addItem(orderId, data) {
        return prisma_1.prisma.$transaction(async (tx) => {
            const order = await tx.order.findUnique({
                where: { id: orderId },
                select: { platformMarkupPct: true },
            });
            if (!order)
                throw new Error("Pedido no existe");
            const markupMultiplier = this.calculateMarkupMultiplier(order.platformMarkupPct);
            // 1) Trae producto/variante y valida disponibilidad
            const product = await tx.product.findUnique({
                where: { id: data.productId },
                include: { variants: true, comboItemsAsCombo: true },
            });
            if (!product || !product.isActive || !product.isAvailable) {
                throw new Error("Producto no disponible");
            }
            let basePrice = 0;
            let variantName = null;
            if (product.type === "VARIANTED") {
                if (!data.variantId)
                    throw new Error("variantId requerido para producto VARIANTED");
                const variant = await tx.productVariant.findUnique({ where: { id: data.variantId } });
                if (!variant || !variant.isActive || !variant.isAvailable) {
                    throw new Error("Variante no disponible");
                }
                basePrice = variant.priceCents;
                variantName = variant.name;
            }
            else {
                basePrice = product.priceCents ?? 0;
            }
            basePrice = this.applyMarkup(basePrice, markupMultiplier);
            const { extras, mods } = await this.resolveModifiers(tx, data.modifiers, markupMultiplier);
            const totalUnit = basePrice + extras;
            const total = totalUnit * data.quantity;
            // 3) Crea renglón principal
            const item = await tx.orderItem.create({
                data: {
                    orderId,
                    productId: product.id,
                    variantId: data.variantId ?? null,
                    quantity: data.quantity,
                    status: "NEW",
                    basePriceCents: basePrice,
                    extrasTotalCents: extras,
                    totalPriceCents: total,
                    nameSnapshot: product.name,
                    variantNameSnapshot: variantName,
                    notes: data.notes ?? null,
                    modifiers: mods.length ? { createMany: { data: mods } } : undefined,
                },
            });
            // 4) Si es combo, crea hijos
            if (data.children?.length) {
                await this.createCustomChildItems(tx, orderId, item.id, data.quantity, data.children, markupMultiplier);
            }
            else if (product.type === "COMBO" && product.comboItemsAsCombo.length) {
                for (const ci of product.comboItemsAsCombo) {
                    const comp = await tx.product.findUnique({ where: { id: ci.componentProductId } });
                    if (!comp)
                        continue;
                    let childVariantId = null;
                    let childVariantName = null;
                    if (ci.componentVariantId) {
                        const childVariant = await tx.productVariant.findUnique({
                            where: { id: ci.componentVariantId },
                        });
                        if (childVariant && childVariant.isActive && childVariant.isAvailable) {
                            childVariantId = childVariant.id;
                            childVariantName = childVariant.name;
                        }
                    }
                    await tx.orderItem.create({
                        data: {
                            orderId,
                            productId: ci.componentProductId,
                            parentItemId: item.id,
                            quantity: (ci.quantity || 1) * data.quantity,
                            status: "NEW",
                            basePriceCents: 0,
                            extrasTotalCents: 0,
                            totalPriceCents: 0,
                            nameSnapshot: comp.name,
                            variantId: childVariantId,
                            variantNameSnapshot: childVariantName,
                        },
                    });
                }
            }
            // 5) Recalcula totales del pedido
            await this.recalcTotalsTx(tx, orderId);
            return { item };
        });
    }
    async updateItemStatus(orderItemId, status) {
        await prisma_1.prisma.$transaction(async (tx) => {
            const item = await tx.orderItem.findUnique({
                where: { id: orderItemId },
                select: {
                    id: true,
                    status: true,
                    orderId: true,
                    parentItemId: true,
                    order: {
                        select: {
                            id: true,
                            status: true,
                            acceptedAt: true,
                            readyAt: true,
                            servedAt: true,
                        },
                    },
                },
            });
            if (!item) {
                throw new Error("Order item no encontrado");
            }
            if (item.status === status) {
                return;
            }
            const transitions = {
                NEW: ["IN_PROGRESS", "CANCELED"],
                IN_PROGRESS: ["READY", "CANCELED"],
                READY: ["SERVED", "CANCELED"],
                SERVED: [],
                CANCELED: [],
            };
            const allowed = transitions[item.status] || [];
            if (!allowed.includes(status)) {
                throw new Error(`Transición inválida: ${item.status} → ${status}`);
            }
            await tx.orderItem.update({
                where: { id: orderItemId },
                data: { status: status },
            });
            if (status === "CANCELED") {
                await tx.orderItem.updateMany({
                    where: { parentItemId: orderItemId },
                    data: { status: "CANCELED" },
                });
            }
            const orderItems = await tx.orderItem.findMany({
                where: { orderId: item.orderId },
                select: { status: true },
            });
            const allReadyOrServed = orderItems.every((it) => ["READY", "SERVED", "CANCELED"].includes(it.status));
            const allServed = orderItems.every((it) => ["SERVED", "CANCELED"].includes(it.status));
            const progressedStatuses = new Set(["IN_PROGRESS", "READY", "SERVED"]);
            const hasProgress = orderItems.some((it) => progressedStatuses.has(it.status));
            const orderUpdate = {};
            const now = new Date();
            if (!item.order?.acceptedAt && hasProgress) {
                orderUpdate.acceptedAt = now;
            }
            if (allReadyOrServed) {
                if (!item.order?.readyAt) {
                    orderUpdate.readyAt = now;
                }
            }
            else if (item.order?.readyAt) {
                orderUpdate.readyAt = null;
            }
            if (allServed) {
                if (!item.order?.servedAt) {
                    orderUpdate.servedAt = now;
                }
            }
            else if (item.order?.servedAt) {
                orderUpdate.servedAt = null;
            }
            if (Object.keys(orderUpdate).length) {
                await tx.order.update({
                    where: { id: item.orderId },
                    data: orderUpdate,
                });
            }
            if (status === "CANCELED") {
                await this.recalcTotalsTx(tx, item.orderId);
            }
        });
    }
    async addPayment(orderId, data) {
        const payment = await prisma_1.prisma.$transaction(async (tx) => {
            const order = await tx.order.findUnique({ where: { id: orderId }, select: { totalCents: true } });
            if (!order)
                throw new Error("Pedido no existe");
            const prevAgg = await tx.payment.aggregate({
                where: { orderId },
                _sum: { amountCents: true, tipCents: true },
            });
            const alreadyPaid = (prevAgg._sum.amountCents || 0) + (prevAgg._sum.tipCents || 0);
            if (alreadyPaid >= (order.totalCents || 0)) {
                throw new Error("Pedido ya pagado");
            }
            const pay = await tx.payment.create({
                data: {
                    orderId,
                    method: data.method,
                    amountCents: data.amountCents,
                    tipCents: data.tipCents || 0,
                    receivedByUserId: data.receivedByUserId,
                    receivedAmountCents: data.receivedAmountCents ?? null,
                    changeCents: data.changeCents ?? null,
                    note: data.note ?? null,
                },
            });
            // Suma pagos y cierra si está cubierto
            const agg = await tx.payment.aggregate({
                where: { orderId },
                _sum: { amountCents: true, tipCents: true },
            });
            const ord = await tx.order.findUnique({ where: { id: orderId } });
            const paid = (agg._sum.amountCents || 0) + (agg._sum.tipCents || 0);
            if (ord && paid >= (ord.totalCents || 0)) {
                await tx.order.update({
                    where: { id: orderId },
                    data: { status: "CLOSED", closedAt: new Date() },
                });
            }
            return pay;
        });
        return payment;
    }
    async recalcTotals(orderId) {
        await prisma_1.prisma.$transaction(async (tx) => this.recalcTotalsTx(tx, orderId));
    }
    // 🔧 IMPORTANTE: usar Prisma.TransactionClient aquí
    async recalcTotalsTx(tx, orderId) {
        const [items, orderSnapshot] = await Promise.all([
            tx.orderItem.findMany({
                where: {
                    orderId,
                    status: { not: "CANCELED" },
                },
                select: { totalPriceCents: true },
            }),
            tx.order.findUnique({
                where: { id: orderId },
                select: {
                    discountCents: true,
                    serviceFeeCents: true,
                    taxCents: true,
                },
            }),
        ]);
        const subtotal = items.reduce((acc, it) => acc + (it.totalPriceCents || 0), 0);
        const discount = orderSnapshot?.discountCents ?? 0;
        const serviceFee = orderSnapshot?.serviceFeeCents ?? 0;
        const tax = orderSnapshot?.taxCents ?? 0;
        const total = Math.max(0, subtotal - discount + serviceFee + tax);
        await tx.order.update({
            where: { id: orderId },
            data: {
                subtotalCents: subtotal,
                discountCents: discount,
                serviceFeeCents: serviceFee,
                taxCents: tax,
                totalCents: total,
            },
        });
    }
    async listKDS(filters) {
        const validStatusSet = new Set(["NEW", "IN_PROGRESS", "READY"]);
        const statusesInput = filters.statuses?.filter((s) => validStatusSet.has(s)) ?? [];
        const statusesToQuery = statusesInput.length ? statusesInput : ["NEW", "IN_PROGRESS"];
        const where = {
            items: {
                some: {
                    parentItemId: null,
                    status: { in: statusesToQuery },
                },
            },
        };
        if (filters.serviceType) {
            where.serviceType = filters.serviceType;
        }
        if (filters.source) {
            where.source = filters.source;
        }
        if (filters.tableId !== undefined) {
            where.tableId = filters.tableId;
        }
        if (filters.from || filters.to) {
            where.openedAt = {
                ...(filters.from ? { gte: filters.from } : {}),
                ...(filters.to ? { lte: filters.to } : {}),
            };
        }
        const orders = await prisma_1.prisma.order.findMany({
            where,
            include: {
                table: { select: { id: true, name: true } },
                items: {
                    where: {
                        parentItemId: null,
                        status: { in: statusesToQuery },
                    },
                    include: {
                        product: { select: { id: true, name: true, type: true } },
                        variant: { select: { id: true, name: true } },
                        modifiers: true,
                        childItems: {
                            include: {
                                product: { select: { id: true, name: true, type: true } },
                                variant: { select: { id: true, name: true } },
                                modifiers: true,
                            },
                            orderBy: { id: "asc" },
                        },
                    },
                    orderBy: { id: "asc" },
                },
            },
            orderBy: [{ openedAt: "asc" }, { id: "asc" }],
        });
        const mapModifiers = (mods) => mods.map((m) => ({
            name: m.nameSnapshot,
            quantity: m.quantity,
        }));
        const tickets = orders
            .map((order) => {
            const items = order.items.map((item) => {
                const children = item.childItems.map((child) => ({
                    id: child.id,
                    productId: child.productId,
                    name: child.nameSnapshot,
                    variantName: child.variantNameSnapshot,
                    quantity: child.quantity,
                    status: child.status,
                    notes: child.notes ?? null,
                    modifiers: mapModifiers(child.modifiers),
                }));
                return {
                    id: item.id,
                    productId: item.productId,
                    name: item.nameSnapshot,
                    variantName: item.variantNameSnapshot,
                    quantity: item.quantity,
                    status: item.status,
                    notes: item.notes ?? null,
                    modifiers: mapModifiers(item.modifiers),
                    isComboParent: item.product?.type === "COMBO",
                    children,
                };
            });
            return {
                orderId: order.id,
                code: order.code,
                status: order.status,
                serviceType: order.serviceType,
                source: order.source,
                openedAt: order.openedAt,
                note: order.note ?? null,
                prepEtaMinutes: order.prepEtaMinutes ?? null,
                customerName: order.customerName ?? null,
                customerPhone: order.customerPhone ?? null,
                table: order.table ? { id: order.table.id, name: order.table.name } : null,
                items,
            };
        })
            .filter((ticket) => ticket.items.length > 0);
        return tickets;
    }
    async getPaymentsReport(filters) {
        const paymentWhere = {};
        if (filters.from || filters.to) {
            paymentWhere.paidAt = {
                ...(filters.from ? { gte: filters.from } : {}),
                ...(filters.to ? { lte: filters.to } : {}),
            };
        }
        const payments = await prisma_1.prisma.payment.findMany({ where: paymentWhere, select: {
                method: true,
                amountCents: true,
                tipCents: true,
                changeCents: true,
                receivedAmountCents: true,
            } });
        const totalsMap = new Map();
        for (const pay of payments) {
            const method = pay.method;
            if (!totalsMap.has(method)) {
                totalsMap.set(method, {
                    method,
                    paymentsCount: 0,
                    amountCents: 0,
                    tipCents: 0,
                    changeCents: 0,
                    receivedAmountCents: 0,
                });
            }
            const bucket = totalsMap.get(method);
            bucket.paymentsCount += 1;
            bucket.amountCents += pay.amountCents || 0;
            bucket.tipCents += pay.tipCents || 0;
            bucket.changeCents += pay.changeCents || 0;
            bucket.receivedAmountCents += pay.receivedAmountCents || 0;
        }
        const totalsByMethod = Array.from(totalsMap.values()).sort((a, b) => a.method.localeCompare(b.method));
        const ordersClosedWhere = { status: "CLOSED" };
        if (filters.from || filters.to) {
            ordersClosedWhere.closedAt = {
                ...(filters.from ? { gte: filters.from } : {}),
                ...(filters.to ? { lte: filters.to } : {}),
            };
        }
        const ordersClosed = await prisma_1.prisma.order.count({ where: ordersClosedWhere });
        const ordersCanceledWhere = { status: "CANCELED" };
        if (filters.from || filters.to) {
            ordersCanceledWhere.canceledAt = {
                ...(filters.from ? { gte: filters.from } : {}),
                ...(filters.to ? { lte: filters.to } : {}),
            };
        }
        const ordersCanceled = await prisma_1.prisma.order.count({ where: ordersCanceledWhere });
        const ordersRefundedWhere = { status: "REFUNDED" };
        if (filters.from || filters.to) {
            ordersRefundedWhere.refundedAt = {
                ...(filters.from ? { gte: filters.from } : {}),
                ...(filters.to ? { lte: filters.to } : {}),
            };
        }
        const ordersRefunded = await prisma_1.prisma.order.count({ where: ordersRefundedWhere });
        const grandTotals = totalsByMethod.reduce((acc, curr) => {
            acc.paymentsCount += curr.paymentsCount;
            acc.amountCents += curr.amountCents;
            acc.tipCents += curr.tipCents;
            acc.changeCents += curr.changeCents;
            acc.receivedAmountCents += curr.receivedAmountCents;
            return acc;
        }, { paymentsCount: 0, amountCents: 0, tipCents: 0, changeCents: 0, receivedAmountCents: 0, ordersClosed, ordersCanceled, ordersRefunded });
        let ordersDetails;
        if (filters.includeOrders) {
            const orders = await prisma_1.prisma.order.findMany({
                where: {
                    OR: [
                        ordersClosedWhere,
                        ordersCanceledWhere,
                        ordersRefundedWhere,
                    ],
                },
                include: {
                    table: { select: { id: true, name: true } },
                    payments: {
                        select: {
                            id: true,
                            method: true,
                            amountCents: true,
                            tipCents: true,
                            changeCents: true,
                            paidAt: true,
                        },
                        orderBy: { paidAt: "asc" },
                    },
                },
                orderBy: [{ closedAt: "asc" }, { id: "asc" }],
            });
            ordersDetails = orders.map((order) => ({
                orderId: order.id,
                code: order.code,
                status: order.status,
                serviceType: order.serviceType,
                source: order.source,
                openedAt: order.openedAt,
                closedAt: order.closedAt,
                subtotalCents: order.subtotalCents,
                totalCents: order.totalCents,
                note: order.note ?? null,
                table: order.table ? { id: order.table.id, name: order.table.name } : null,
                payments: order.payments.map((p) => ({
                    id: p.id,
                    method: p.method,
                    amountCents: p.amountCents,
                    tipCents: p.tipCents,
                    changeCents: p.changeCents ?? null,
                    paidAt: p.paidAt,
                })),
            }));
        }
        return {
            range: {
                from: filters.from,
                to: filters.to,
            },
            totalsByMethod,
            grandTotals,
            orders: ordersDetails,
        };
    }
    async updateItem(orderItemId, data) {
        await prisma_1.prisma.$transaction(async (tx) => {
            const current = await tx.orderItem.findUnique({
                where: { id: orderItemId },
                include: {
                    product: { include: { comboItemsAsCombo: true } },
                    modifiers: true,
                    childItems: true,
                    order: { select: { platformMarkupPct: true } },
                }
            });
            if (!current)
                throw new Error("Order item no encontrado");
            const markupMultiplier = this.calculateMarkupMultiplier(current.order?.platformMarkupPct ?? null);
            // 1) Reemplazo de modificadores (opcional)
            let extras = current.extrasTotalCents || 0;
            if (data.replaceModifiers) {
                // recalcular extras desde cero
                const ids = data.replaceModifiers.map(m => m.optionId);
                const options = await tx.modifierOption.findMany({ where: { id: { in: ids } } });
                // borra los anteriores
                await tx.orderItemModifier.deleteMany({ where: { orderItemId } });
                // crea los nuevos
                const toCreate = data.replaceModifiers.map(m => {
                    const opt = options.find(o => o.id === m.optionId);
                    if (!opt)
                        throw new Error(`Modifier option ${m.optionId} inválido`);
                    const qty = m.quantity ?? 1;
                    const priceExtraCents = this.applyMarkup(opt.priceExtraCents || 0, markupMultiplier);
                    return {
                        orderItemId,
                        optionId: opt.id,
                        quantity: qty,
                        priceExtraCents,
                        nameSnapshot: opt.name
                    };
                });
                if (toCreate.length) {
                    await tx.orderItemModifier.createMany({ data: toCreate });
                }
                // suma extras
                extras = toCreate.reduce((acc, it) => acc + it.priceExtraCents * it.quantity, 0);
            }
            else {
                // si no se reemplazaron, manten los actuales
                extras = (await tx.orderItemModifier.findMany({ where: { orderItemId } }))
                    .reduce((acc, it) => acc + it.priceExtraCents * it.quantity, 0);
            }
            // 2) Actualizar quantity y notas
            const newQty = data.quantity ?? current.quantity;
            const base = current.basePriceCents || 0;
            const totalUnit = base + extras;
            const newTotal = totalUnit * newQty;
            await tx.orderItem.update({
                where: { id: orderItemId },
                data: {
                    quantity: newQty,
                    notes: data.notes ?? current.notes ?? null,
                    extrasTotalCents: extras,
                    totalPriceCents: newTotal
                }
            });
            // 3) Si es combo (item padre), escalar cantidades de hijos proporcionalmente
            if (current.childItems?.length) {
                const oldQty = current.quantity || 1;
                if (oldQty !== newQty && oldQty > 0) {
                    for (const child of current.childItems) {
                        const perParent = Math.max(1, Math.round(child.quantity / oldQty)); // aproximar
                        const newChildQty = perParent * newQty;
                        await tx.orderItem.update({
                            where: { id: child.id },
                            data: { quantity: newChildQty }
                        });
                    }
                }
            }
            await this.recalcTotalsTx(tx, current.orderId);
        });
    }
    async removeItem(orderItemId) {
        await prisma_1.prisma.$transaction(async (tx) => {
            const item = await tx.orderItem.findUnique({
                where: { id: orderItemId },
                select: { id: true, orderId: true, parentItemId: true }
            });
            if (!item)
                return;
            // borra modifiers del item y de los hijos
            await tx.orderItemModifier.deleteMany({
                where: { OR: [{ orderItemId }, { orderItem: { parentItemId: orderItemId } }] }
            });
            // borra hijos si los hay
            await tx.orderItem.deleteMany({ where: { parentItemId: orderItemId } });
            // borra el item
            await tx.orderItem.delete({ where: { id: orderItemId } });
            await this.recalcTotalsTx(tx, item.orderId);
        });
    }
    async splitOrderByItems(sourceOrderId, input) {
        return prisma_1.prisma.$transaction(async (tx) => {
            // 1) Trae items y valida que pertenecen al pedido
            const items = await tx.orderItem.findMany({
                where: { id: { in: input.itemIds } },
                select: { id: true, orderId: true, parentItemId: true }
            });
            if (!items.length)
                throw new Error("Sin items para dividir");
            if (items.some(i => i.orderId !== sourceOrderId)) {
                throw new Error("Todos los items deben pertenecer al mismo pedido");
            }
            // 2) Expandir para combos: si uno es padre, incluye todos sus hijos.
            const parentIds = items.filter(i => !i.parentItemId).map(i => i.id);
            const childIds = items.filter(i => i.parentItemId).map(i => i.id);
            // Si enviaron un hijo sin el padre → también movemos el padre y todos sus hijos
            let fullMoveIds = new Set(items.map(i => i.id));
            // incluye hijos de todos los padres seleccionados
            if (parentIds.length) {
                const allChilds = await tx.orderItem.findMany({
                    where: { parentItemId: { in: parentIds } },
                    select: { id: true }
                });
                for (const c of allChilds)
                    fullMoveIds.add(c.id);
            }
            // Si hay hijos sin su padre en la selección, trae a sus padres y a todos los hermanos
            if (childIds.length) {
                const theirParents = await tx.orderItem.findMany({
                    where: { id: { in: childIds } },
                    select: { parentItemId: true }
                });
                const pIds = [...new Set(theirParents.map(p => p.parentItemId).filter(Boolean))];
                if (pIds.length) {
                    const theParents = await tx.orderItem.findMany({ where: { id: { in: pIds } }, select: { id: true } });
                    for (const p of theParents)
                        fullMoveIds.add(p.id);
                    const siblings = await tx.orderItem.findMany({ where: { parentItemId: { in: pIds } }, select: { id: true } });
                    for (const s of siblings)
                        fullMoveIds.add(s.id);
                }
            }
            const idsArray = Array.from(fullMoveIds);
            // 3) Crear el nuevo pedido con metadatos (usa los del pedido original si no mandan override)
            const src = await tx.order.findUnique({ where: { id: sourceOrderId } });
            if (!src)
                throw new Error("Pedido origen no existe");
            const newOrder = await tx.order.create({
                data: {
                    code: await this.generateSequentialCode(tx, input.servedByUserId),
                    status: "OPEN",
                    serviceType: (input.serviceType ?? src.serviceType),
                    source: src.source ?? "POS",
                    externalRef: null,
                    prepEtaMinutes: src.prepEtaMinutes ?? null,
                    tableId: input.tableId ?? src.tableId ?? null,
                    note: input.note ?? null,
                    covers: input.covers ?? src.covers ?? null,
                    servedByUserId: input.servedByUserId,
                    platformMarkupPct: src.platformMarkupPct ?? null,
                    acceptedAt: new Date(),
                }
            });
            // 4) Mover los items (actualiza orderId; parent/child siguen apuntando a sus ids originales que también fueron movidos)
            await tx.orderItem.updateMany({
                where: { id: { in: idsArray } },
                data: { orderId: newOrder.id }
            });
            // 5) Recalcular totales en ambos pedidos
            await this.recalcTotalsTx(tx, sourceOrderId);
            await this.recalcTotalsTx(tx, newOrder.id);
            return { newOrderId: newOrder.id, code: newOrder.code };
        });
    }
    async updateMeta(orderId, data) {
        const updateData = {};
        if (data.tableId !== undefined) {
            updateData.tableId = data.tableId ?? null;
        }
        if (data.note !== undefined) {
            updateData.note = data.note ?? null;
        }
        if (data.covers !== undefined) {
            updateData.covers = data.covers ?? null;
        }
        if (data.prepEtaMinutes !== undefined) {
            updateData.prepEtaMinutes = data.prepEtaMinutes ?? null;
        }
        if (!Object.keys(updateData).length) {
            return;
        }
        await prisma_1.prisma.order.update({
            where: { id: orderId },
            data: updateData,
        });
    }
    async updateStatus(orderId, status) {
        await prisma_1.prisma.$transaction(async (tx) => {
            const ord = await tx.order.findUnique({
                where: { id: orderId },
                select: {
                    status: true,
                    totalCents: true,
                    acceptedAt: true,
                    closedAt: true,
                    canceledAt: true,
                },
            });
            if (!ord)
                throw new Error("Pedido no existe");
            const from = ord.status;
            const to = status;
            if (from === "CLOSED" || from === "CANCELED" || from === "REFUNDED") {
                throw new Error(`No se puede cambiar estado desde ${from}`);
            }
            const allowMap = {
                DRAFT: ["OPEN", "HOLD", "CANCELED"],
                OPEN: ["HOLD", "CANCELED", "CLOSED"],
                HOLD: ["OPEN", "CANCELED"],
                CLOSED: [],
                CANCELED: [],
                REFUNDED: [],
            };
            const allowed = allowMap[from] || [];
            if (!allowed.includes(to)) {
                throw new Error(`Transición inválida: ${from} → ${to}`);
            }
            const now = new Date();
            const updateData = { status: to };
            if (to === "OPEN" && !ord.acceptedAt) {
                updateData.acceptedAt = now;
            }
            if (to === "CLOSED") {
                await this.recalcTotalsTx(tx, orderId);
                const agg = await tx.payment.aggregate({
                    where: { orderId },
                    _sum: { amountCents: true, tipCents: true },
                });
                const paid = (agg._sum.amountCents || 0) + (agg._sum.tipCents || 0);
                const total = (await tx.order.findUnique({
                    where: { id: orderId },
                    select: { totalCents: true },
                }))?.totalCents ?? 0;
                if (paid < total) {
                    throw new Error(`No se puede cerrar: pagado ${paid} < total ${total}`);
                }
                updateData.closedAt = now;
            }
            else {
                updateData.closedAt = null;
            }
            if (status === "CANCELED") {
                updateData.canceledAt = now;
            }
            else if (ord.canceledAt && status !== "CANCELED") {
                updateData.canceledAt = null;
            }
            await tx.order.update({
                where: { id: orderId },
                data: updateData,
            });
        });
    }
    async list(filters) {
        const where = {};
        if (filters.statuses && filters.statuses.length) {
            where.status = { in: filters.statuses };
        }
        if (filters.serviceType) {
            where.serviceType = filters.serviceType;
        }
        if (filters.source) {
            where.source = { equals: filters.source };
        }
        if (filters.tableId !== undefined) {
            where.tableId = { equals: filters.tableId };
        }
        if (filters.from || filters.to) {
            where.openedAt = {
                ...(filters.from ? { gte: filters.from } : {}),
                ...(filters.to ? { lte: filters.to } : {}),
            };
        }
        if (filters.search?.trim()) {
            const search = filters.search.trim();
            where.OR = [
                { code: { contains: search, mode: "insensitive" } },
                { note: { contains: search, mode: "insensitive" } },
                { customerName: { contains: search, mode: "insensitive" } },
                { customerPhone: { contains: search, mode: "insensitive" } },
            ];
        }
        const orders = await prisma_1.prisma.order.findMany({
            where,
            include: {
                table: { select: { id: true, name: true } },
                servedBy: { select: { id: true, name: true, email: true } },
                payments: {
                    select: {
                        id: true,
                        method: true,
                        amountCents: true,
                        tipCents: true,
                        paidAt: true,
                        note: true,
                    },
                    orderBy: { paidAt: "asc" },
                },
            },
            orderBy: [{ openedAt: "desc" }],
        });
        return orders.map((order) => {
            const paymentsTotal = order.payments.reduce((acc, p) => acc + p.amountCents, 0);
            const tipsTotal = order.payments.reduce((acc, p) => acc + p.tipCents, 0);
            return {
                id: order.id,
                code: order.code,
                status: order.status,
                serviceType: order.serviceType,
                source: order.source ?? "POS",
                servedByUserId: order.servedByUserId ?? null,
                servedBy: order.servedBy
                    ? {
                        id: order.servedBy.id,
                        name: order.servedBy.name ?? null,
                        email: order.servedBy.email ?? null,
                    }
                    : null,
                platformMarkupPct: order.platformMarkupPct !== null && order.platformMarkupPct !== undefined
                    ? Number(order.platformMarkupPct)
                    : null,
                subtotalCents: order.subtotalCents,
                discountCents: order.discountCents,
                serviceFeeCents: order.serviceFeeCents,
                taxCents: order.taxCents,
                totalCents: order.totalCents,
                paymentsTotalCents: paymentsTotal,
                paymentsTipCents: tipsTotal,
                openedAt: order.openedAt,
                closedAt: order.closedAt,
                acceptedAt: order.acceptedAt ?? null,
                readyAt: order.readyAt ?? null,
                servedAt: order.servedAt ?? null,
                canceledAt: order.canceledAt ?? null,
                prepEtaMinutes: order.prepEtaMinutes ?? null,
                externalRef: order.externalRef ?? null,
                table: order.table ? { id: order.table.id, name: order.table.name } : null,
                payments: order.payments.map((p) => ({
                    id: p.id,
                    method: p.method,
                    amountCents: p.amountCents,
                    tipCents: p.tipCents,
                    paidAt: p.paidAt,
                    note: p.note,
                })),
            };
        });
    }
    async getTodayOrderCount(tx, referenceDate) {
        const start = new Date(referenceDate);
        start.setHours(0, 0, 0, 0);
        const end = new Date(referenceDate);
        end.setHours(23, 59, 59, 999);
        return tx.order.count({
            where: {
                openedAt: {
                    gte: start,
                    lte: end,
                },
            },
        });
    }
    async generateSequentialCode(tx, servedByUserId) {
        const now = new Date();
        const noteNumber = (await this.getTodayOrderCount(tx, now)) + 1;
        let attendedBy;
        if (servedByUserId != null) {
            const user = await tx.user.findUnique({
                where: { id: servedByUserId },
                select: { id: true, name: true },
            });
            attendedBy = { id: user?.id ?? servedByUserId, name: user?.name ?? null };
        }
        return formatNoteCode(noteNumber, now, attendedBy);
    }
    calculateMarkupMultiplier(markup) {
        if (markup === null || markup === undefined)
            return 1;
        const value = Number(markup);
        if (!Number.isFinite(value) || value === 0)
            return 1;
        return 1 + value / 100;
    }
    applyMarkup(amount, multiplier) {
        if (!amount || multiplier === 1)
            return amount;
        return Math.round(amount * multiplier);
    }
    async getMarkupForSource(tx, source) {
        if (!source || source === "POS")
            return null;
        const config = await tx.channelConfig.findUnique({
            where: { source },
            select: { markupPct: true },
        });
        return config ? Number(config.markupPct) : null;
    }
    async resolveModifiers(tx, modifiers, markupMultiplier) {
        if (!modifiers?.length)
            return { extras: 0, mods: [] };
        const ids = modifiers.map((mm) => mm.optionId);
        const options = await tx.modifierOption.findMany({ where: { id: { in: ids } } });
        let extras = 0;
        const mods = [];
        for (const m of modifiers) {
            const opt = options.find((o) => o.id === m.optionId);
            if (!opt)
                continue;
            const qty = m.quantity ?? 1;
            const unitExtra = this.applyMarkup(opt.priceExtraCents || 0, markupMultiplier);
            extras += unitExtra * qty;
            mods.push({
                optionId: opt.id,
                quantity: qty,
                priceExtraCents: unitExtra,
                nameSnapshot: opt.name,
            });
        }
        return { extras, mods };
    }
    async createCustomChildItems(tx, orderId, parentItemId, parentQuantity, children, markupMultiplier) {
        for (const child of children) {
            const product = await tx.product.findUnique({
                where: { id: child.productId },
            });
            if (!product || !product.isActive || !product.isAvailable) {
                throw new Error("Producto hijo no disponible");
            }
            let variantId = null;
            let variantName = null;
            if (product.type === "VARIANTED") {
                if (!child.variantId)
                    throw new Error("variantId requerido para componente VARIANTED");
                const variant = await tx.productVariant.findUnique({ where: { id: child.variantId } });
                if (!variant || !variant.isActive || !variant.isAvailable) {
                    throw new Error("Variante de componente no disponible");
                }
                variantId = variant.id;
                variantName = variant.name;
            }
            else if (child.variantId) {
                const variant = await tx.productVariant.findUnique({ where: { id: child.variantId } });
                if (variant && variant.isActive && variant.isAvailable) {
                    variantId = variant.id;
                    variantName = variant.name;
                }
            }
            const childQty = (child.quantity ?? 1) * parentQuantity;
            const { extras, mods } = await this.resolveModifiers(tx, child.modifiers, markupMultiplier);
            const totalUnit = extras;
            const totalPrice = totalUnit * childQty;
            await tx.orderItem.create({
                data: {
                    orderId,
                    productId: product.id,
                    parentItemId,
                    variantId,
                    quantity: childQty,
                    status: "NEW",
                    basePriceCents: 0,
                    extrasTotalCents: extras,
                    totalPriceCents: totalPrice,
                    nameSnapshot: product.name,
                    variantNameSnapshot: variantName,
                    notes: child.notes ?? null,
                    modifiers: mods.length ? { createMany: { data: mods } } : undefined,
                },
            });
        }
    }
    async refundOrder(orderId, input) {
        return prisma_1.prisma.$transaction(async (tx) => {
            const order = await tx.order.findUnique({
                where: { id: orderId },
                include: { payments: true },
            });
            if (!order)
                throw new Error("Pedido no existe");
            if (order.status !== "CLOSED") {
                throw new Error("Solo puedes reembolsar pedidos cerrados");
            }
            if (!order.payments.length) {
                throw new Error("Pedido sin pagos a reembolsar");
            }
            const reasonSuffix = input.reason ? `: ${input.reason}` : "";
            for (const payment of order.payments) {
                await tx.payment.create({
                    data: {
                        orderId,
                        method: payment.method,
                        amountCents: -(payment.amountCents || 0),
                        tipCents: -(payment.tipCents || 0),
                        changeCents: payment.changeCents ? -payment.changeCents : null,
                        receivedAmountCents: payment.receivedAmountCents
                            ? -payment.receivedAmountCents
                            : null,
                        receivedByUserId: input.adminUserId,
                        note: `REFUND${reasonSuffix}`.trim(),
                    },
                });
            }
            const totalAmount = order.payments.reduce((acc, p) => acc + (p.amountCents || 0), 0);
            const totalTips = order.payments.reduce((acc, p) => acc + (p.tipCents || 0), 0);
            await tx.orderRefundLog.create({
                data: {
                    orderId,
                    adminUserId: input.adminUserId,
                    amountCents: totalAmount,
                    tipCents: totalTips,
                    reason: input.reason ?? null,
                },
            });
            const updated = await tx.order.update({
                where: { id: orderId },
                data: { status: "REFUNDED", refundedAt: new Date() },
                select: { id: true, status: true, refundedAt: true },
            });
            return {
                orderId: updated.id,
                status: updated.status,
                refundedAt: updated.refundedAt,
            };
        });
    }
}
exports.PrismaOrderRepository = PrismaOrderRepository;
//# sourceMappingURL=PrismaOrderRepository.js.map