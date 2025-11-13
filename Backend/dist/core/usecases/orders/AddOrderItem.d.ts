import type { IOrderRepository } from "../../domain/repositories/IOrderRepository";
export declare class AddOrderItem {
    private repo;
    constructor(repo: IOrderRepository);
    exec(orderId: number, data: Parameters<IOrderRepository["addItem"]>[1]): Promise<{
        item: import(".prisma/client").OrderItem;
    }>;
}
//# sourceMappingURL=AddOrderItem.d.ts.map