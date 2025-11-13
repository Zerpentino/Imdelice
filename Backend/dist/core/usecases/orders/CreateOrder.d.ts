import type { IOrderRepository } from "../../domain/repositories/IOrderRepository";
export declare class CreateOrder {
    private repo;
    constructor(repo: IOrderRepository);
    exec(input: Parameters<IOrderRepository["create"]>[0], servedByUserId: number): Promise<{
        id: number;
        code: string;
        status: import(".prisma/client").$Enums.OrderStatus;
        serviceType: import(".prisma/client").$Enums.ServiceType;
        source: import(".prisma/client").$Enums.OrderSource;
        externalRef: string | null;
        prepEtaMinutes: number | null;
        platformMarkupPct: import("@prisma/client/runtime/library").Decimal | null;
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
    }>;
}
//# sourceMappingURL=CreateOrder.d.ts.map