import type { IOrderRepository } from "../../domain/repositories/IOrderRepository";
export declare class AddPayment {
    private repo;
    constructor(repo: IOrderRepository);
    exec(orderId: number, data: Parameters<IOrderRepository["addPayment"]>[1]): Promise<{
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
    }>;
}
//# sourceMappingURL=AddPayment.d.ts.map