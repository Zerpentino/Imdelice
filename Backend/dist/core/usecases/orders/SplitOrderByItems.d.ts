import type { IOrderRepository } from "../../domain/repositories/IOrderRepository";
export declare class SplitOrderByItems {
    private repo;
    constructor(repo: IOrderRepository);
    exec(sourceOrderId: number, input: {
        itemIds: number[];
        servedByUserId: number;
        serviceType?: "DINE_IN" | "TAKEAWAY" | "DELIVERY";
        tableId?: number | null;
        note?: string | null;
        covers?: number | null;
    }): Promise<{
        newOrderId: number;
        code: string;
    }>;
}
//# sourceMappingURL=SplitOrderByItems.d.ts.map