import type { Order, OrderItem, Payment } from "@prisma/client";
export type OrderStatus = "DRAFT" | "OPEN" | "HOLD" | "CLOSED" | "CANCELED" | "REFUNDED";
export type OrderItemStatus = "NEW" | "IN_PROGRESS" | "READY" | "SERVED" | "CANCELED";
export type ServiceType = "DINE_IN" | "TAKEAWAY" | "DELIVERY";
export type PaymentMethod = "CASH" | "CARD" | "TRANSFER" | "OTHER";
export type OrderSource = "POS" | "UBER" | "DIDI" | "RAPPI";
export interface KDSFilters {
    statuses?: OrderItemStatus[];
    serviceType?: ServiceType;
    source?: OrderSource;
    tableId?: number;
    from?: Date;
    to?: Date;
}
export interface KDSModifierSummary {
    name: string;
    quantity: number;
}
export interface KDSChildItem {
    id: number;
    productId: number;
    name: string;
    variantName?: string | null;
    quantity: number;
    status: OrderItemStatus;
    notes?: string | null;
    modifiers: KDSModifierSummary[];
}
export interface KDSOrderItem {
    id: number;
    productId: number;
    name: string;
    variantName?: string | null;
    quantity: number;
    status: OrderItemStatus;
    notes?: string | null;
    modifiers: KDSModifierSummary[];
    isComboParent: boolean;
    children: KDSChildItem[];
}
export interface KDSOrderTicket {
    orderId: number;
    code: string;
    status: OrderStatus;
    serviceType: ServiceType;
    source: OrderSource;
    openedAt: Date;
    note?: string | null;
    prepEtaMinutes?: number | null;
    customerName?: string | null;
    customerPhone?: string | null;
    table?: {
        id: number;
        name: string | null;
    } | null;
    items: KDSOrderItem[];
}
export interface ListOrdersFilters {
    statuses?: OrderStatus[];
    serviceType?: ServiceType;
    source?: OrderSource;
    from?: Date;
    to?: Date;
    tableId?: number;
    search?: string;
}
export interface ListOrdersItem {
    id: number;
    code: string;
    status: OrderStatus;
    serviceType: ServiceType;
    source: OrderSource;
    servedByUserId?: number | null;
    servedBy?: {
        id: number;
        name: string | null;
        email: string | null;
    } | null;
    platformMarkupPct?: number | null;
    subtotalCents: number;
    discountCents: number;
    serviceFeeCents: number;
    taxCents: number;
    totalCents: number;
    paymentsTotalCents: number;
    paymentsTipCents: number;
    openedAt: Date;
    closedAt: Date | null;
    acceptedAt: Date | null;
    readyAt: Date | null;
    servedAt: Date | null;
    canceledAt: Date | null;
    prepEtaMinutes?: number | null;
    externalRef?: string | null;
    table?: {
        id: number;
        name: string | null;
    } | null;
    payments: Array<{
        id: number;
        method: PaymentMethod;
        amountCents: number;
        tipCents: number;
        paidAt: Date;
        note?: string | null;
    }>;
}
export interface PaymentsReportFilters {
    from?: Date;
    to?: Date;
    includeOrders?: boolean;
}
export interface PaymentsReportMethodSummary {
    method: PaymentMethod;
    paymentsCount: number;
    amountCents: number;
    tipCents: number;
    changeCents: number;
    receivedAmountCents: number;
}
export interface PaymentsReportResult {
    range: {
        from?: Date;
        to?: Date;
    };
    totalsByMethod: PaymentsReportMethodSummary[];
    grandTotals: {
        paymentsCount: number;
        amountCents: number;
        tipCents: number;
        changeCents: number;
        receivedAmountCents: number;
        ordersClosed: number;
        ordersCanceled: number;
        ordersRefunded: number;
    };
    orders?: PaymentsReportOrderSummary[];
}
export interface PaymentsReportOrderSummary {
    orderId: number;
    code: string;
    status: OrderStatus;
    serviceType: ServiceType;
    source: OrderSource;
    openedAt: Date;
    closedAt: Date | null;
    subtotalCents: number;
    totalCents: number;
    note?: string | null;
    table?: {
        id: number;
        name: string | null;
    } | null;
    payments: Array<{
        id: number;
        method: PaymentMethod;
        amountCents: number;
        tipCents: number;
        changeCents: number | null;
        paidAt: Date;
    }>;
}
export interface IOrderRepository {
    create(input: {
        serviceType: ServiceType;
        source?: OrderSource;
        tableId?: number | null;
        note?: string | null;
        covers?: number | null;
        status?: "DRAFT" | "OPEN" | "HOLD";
        externalRef?: string | null;
        prepEtaMinutes?: number | null;
        platformMarkupPct?: number | null;
    }, servedByUserId: number): Promise<Order>;
    getById(orderId: number): Promise<Order | null>;
    addItem(orderId: number, data: {
        productId: number;
        variantId?: number | null;
        quantity: number;
        notes?: string | null;
        modifiers: {
            optionId: number;
            quantity: number;
        }[];
        children?: Array<{
            productId: number;
            variantId?: number | null;
            quantity?: number;
            notes?: string | null;
            modifiers?: {
                optionId: number;
                quantity: number;
            }[];
        }>;
    }): Promise<{
        item: OrderItem;
    }>;
    updateItemStatus(orderItemId: number, status: OrderItemStatus): Promise<void>;
    addPayment(orderId: number, data: {
        method: PaymentMethod;
        amountCents: number;
        tipCents: number;
        receivedByUserId: number;
        receivedAmountCents?: number | null;
        changeCents?: number | null;
        note?: string | null;
    }): Promise<Payment>;
    recalcTotals(orderId: number): Promise<void>;
    listKDS(filters: KDSFilters): Promise<KDSOrderTicket[]>;
    getPaymentsReport(filters: PaymentsReportFilters): Promise<PaymentsReportResult>;
    refundOrder(orderId: number, input: {
        adminUserId: number;
        reason?: string | null;
    }): Promise<{
        orderId: number;
        status: OrderStatus;
        refundedAt?: Date | null;
    }>;
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
}
//# sourceMappingURL=IOrderRepository.d.ts.map