import type { IOrderRepository, ListOrdersFilters, ListOrdersItem, OrderItemStatus, OrderStatus, PaymentMethod, OrderSource, ServiceType, KDSFilters, KDSOrderTicket, PaymentsReportFilters, PaymentsReportResult } from "../../core/domain/repositories/IOrderRepository";
import { Prisma } from "@prisma/client";
import type { Order, OrderItem, Payment } from "@prisma/client";
interface CreateOrderInput {
    serviceType: ServiceType;
    source?: OrderSource;
    tableId?: number | null;
    note?: string | null;
    covers?: number | null;
    status?: Extract<OrderStatus, "DRAFT" | "OPEN" | "HOLD">;
    externalRef?: string | null;
    prepEtaMinutes?: number | null;
    platformMarkupPct?: number | null;
}
interface AddItemModifierInput {
    optionId: number;
    quantity?: number;
}
interface AddItemInput {
    productId: number;
    variantId?: number | null;
    quantity: number;
    notes?: string | null;
    modifiers: AddItemModifierInput[];
    children?: ChildItemInput[];
}
interface ChildItemInput {
    productId: number;
    variantId?: number | null;
    quantity?: number;
    notes?: string | null;
    modifiers?: AddItemModifierInput[];
}
interface AddPaymentInput {
    method: PaymentMethod;
    amountCents: number;
    tipCents: number;
    receivedByUserId: number;
    receivedAmountCents?: number | null;
    changeCents?: number | null;
    note?: string | null;
}
export declare class PrismaOrderRepository implements IOrderRepository {
    create(input: CreateOrderInput, servedByUserId: number): Promise<Order>;
    getById(orderId: number): Prisma.Prisma__OrderClient<({
        table: {
            name: string;
            id: number;
            isActive: boolean;
            seats: number | null;
        } | null;
        items: ({
            product: {
                name: string;
                id: number;
                type: import(".prisma/client").$Enums.ProductType;
            };
            variant: {
                name: string;
                id: number;
                position: number;
                isActive: boolean;
                priceCents: number;
                sku: string | null;
                isAvailable: boolean;
                productId: number;
            } | null;
            parentItem: {
                id: number;
            } | null;
            childItems: ({
                product: {
                    name: string;
                    id: number;
                    type: import(".prisma/client").$Enums.ProductType;
                };
                variant: {
                    name: string;
                    id: number;
                    position: number;
                    isActive: boolean;
                    priceCents: number;
                    sku: string | null;
                    isAvailable: boolean;
                    productId: number;
                } | null;
                parentItem: {
                    id: number;
                } | null;
                modifiers: {
                    id: number;
                    priceExtraCents: number;
                    quantity: number;
                    nameSnapshot: string;
                    orderItemId: number;
                    optionId: number;
                }[];
            } & {
                id: number;
                productId: number;
                variantId: number | null;
                quantity: number;
                notes: string | null;
                status: import(".prisma/client").$Enums.OrderItemStatus;
                basePriceCents: number;
                extrasTotalCents: number;
                totalPriceCents: number;
                nameSnapshot: string;
                variantNameSnapshot: string | null;
                orderId: number;
                parentItemId: number | null;
            })[];
            modifiers: {
                id: number;
                priceExtraCents: number;
                quantity: number;
                nameSnapshot: string;
                orderItemId: number;
                optionId: number;
            }[];
        } & {
            id: number;
            productId: number;
            variantId: number | null;
            quantity: number;
            notes: string | null;
            status: import(".prisma/client").$Enums.OrderItemStatus;
            basePriceCents: number;
            extrasTotalCents: number;
            totalPriceCents: number;
            nameSnapshot: string;
            variantNameSnapshot: string | null;
            orderId: number;
            parentItemId: number | null;
        })[];
        servedBy: {
            name: string | null;
            id: number;
            email: string;
        } | null;
        payments: {
            id: number;
            orderId: number;
            note: string | null;
            method: import(".prisma/client").$Enums.PaymentMethod;
            amountCents: number;
            tipCents: number;
            receivedAmountCents: number | null;
            changeCents: number | null;
            paidAt: Date;
            receivedByUserId: number | null;
        }[];
    } & {
        id: number;
        code: string;
        status: import(".prisma/client").$Enums.OrderStatus;
        serviceType: import(".prisma/client").$Enums.ServiceType;
        source: import(".prisma/client").$Enums.OrderSource;
        externalRef: string | null;
        prepEtaMinutes: number | null;
        platformMarkupPct: Prisma.Decimal | null;
        tableId: number | null;
        openedAt: Date;
        acceptedAt: Date | null;
        readyAt: Date | null;
        servedAt: Date | null;
        closedAt: Date | null;
        canceledAt: Date | null;
        refundedAt: Date | null;
        note: string | null;
        customerName: string | null;
        customerPhone: string | null;
        covers: number | null;
        servedByUserId: number | null;
        subtotalCents: number;
        discountCents: number;
        serviceFeeCents: number;
        taxCents: number;
        totalCents: number;
    }) | null, null, import("@prisma/client/runtime/library").DefaultArgs, Prisma.PrismaClientOptions>;
    addItem(orderId: number, data: AddItemInput): Promise<{
        item: OrderItem;
    }>;
    updateItemStatus(orderItemId: number, status: OrderItemStatus): Promise<void>;
    addPayment(orderId: number, data: AddPaymentInput): Promise<Payment>;
    recalcTotals(orderId: number): Promise<void>;
    private recalcTotalsTx;
    listKDS(filters: KDSFilters): Promise<KDSOrderTicket[]>;
    getPaymentsReport(filters: PaymentsReportFilters): Promise<PaymentsReportResult>;
    updateItem(orderItemId: number, data: {
        quantity?: number;
        notes?: string | null;
        replaceModifiers?: {
            optionId: number;
            quantity?: number;
        }[];
    }): Promise<void>;
    removeItem(orderItemId: number): Promise<void>;
    splitOrderByItems(sourceOrderId: number, input: {
        itemIds: number[];
        servedByUserId: number;
        serviceType?: ServiceType;
        tableId?: number | null;
        note?: string | null;
        covers?: number | null;
    }): Promise<{
        newOrderId: number;
        code: string;
    }>;
    updateMeta(orderId: number, data: {
        tableId?: number | null;
        note?: string | null;
        covers?: number | null;
        prepEtaMinutes?: number | null;
    }): Promise<void>;
    updateStatus(orderId: number, status: OrderStatus): Promise<void>;
    list(filters: ListOrdersFilters): Promise<ListOrdersItem[]>;
    private getTodayOrderCount;
    private generateSequentialCode;
    private calculateMarkupMultiplier;
    private applyMarkup;
    private getMarkupForSource;
    private resolveModifiers;
    private createCustomChildItems;
    refundOrder(orderId: number, input: {
        adminUserId: number;
        reason?: string | null;
    }): Promise<{
        orderId: number;
        status: OrderStatus;
        refundedAt?: Date | null;
    }>;
}
export {};
//# sourceMappingURL=PrismaOrderRepository.d.ts.map