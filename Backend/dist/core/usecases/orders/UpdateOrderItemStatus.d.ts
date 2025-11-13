import type { IOrderRepository } from "../../domain/repositories/IOrderRepository";
export declare class UpdateOrderItemStatus {
    private repo;
    constructor(repo: IOrderRepository);
    exec(orderItemId: number, status: Parameters<IOrderRepository["updateItemStatus"]>[1]): Promise<void>;
}
//# sourceMappingURL=UpdateOrderItemStatus.d.ts.map