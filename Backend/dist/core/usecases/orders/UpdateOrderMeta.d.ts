import type { IOrderRepository } from "../../domain/repositories/IOrderRepository";
export declare class UpdateOrderMeta {
    private repo;
    constructor(repo: IOrderRepository);
    exec(orderId: number, data: {
        tableId?: number | null;
        note?: string | null;
        covers?: number | null;
        prepEtaMinutes?: number | null;
    }): Promise<void>;
}
//# sourceMappingURL=UpdateOrderMeta.d.ts.map