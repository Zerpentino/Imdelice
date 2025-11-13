import type { IOrderRepository } from "../../domain/repositories/IOrderRepository";
export declare class RemoveOrderItem {
    private repo;
    constructor(repo: IOrderRepository);
    exec(orderItemId: number): Promise<void>;
}
//# sourceMappingURL=RemoveOrderItem.d.ts.map