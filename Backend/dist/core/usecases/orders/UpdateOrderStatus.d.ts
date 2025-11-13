import type { IOrderRepository, OrderStatus } from "../../domain/repositories/IOrderRepository";
export declare class UpdateOrderStatus {
    private readonly repo;
    constructor(repo: IOrderRepository);
    exec(orderId: number, status: OrderStatus): Promise<void>;
}
//# sourceMappingURL=UpdateOrderStatus.d.ts.map