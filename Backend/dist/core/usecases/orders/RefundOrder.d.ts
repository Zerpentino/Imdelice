import type { IOrderRepository } from "../../domain/repositories/IOrderRepository";
export declare class RefundOrder {
    private readonly repo;
    constructor(repo: IOrderRepository);
    exec(orderId: number, input: {
        adminUserId: number;
        reason?: string | null;
    }): Promise<{
        orderId: number;
        status: import("../../domain/repositories/IOrderRepository").OrderStatus;
        refundedAt?: Date | null;
    }>;
}
//# sourceMappingURL=RefundOrder.d.ts.map