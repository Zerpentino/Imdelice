import type { IOrderRepository } from "../../domain/repositories/IOrderRepository";
export declare class UpdateOrderItem {
    private repo;
    constructor(repo: IOrderRepository);
    exec(orderItemId: number, data: {
        quantity?: number;
        notes?: string | null;
        replaceModifiers?: {
            optionId: number;
            quantity?: number;
        }[];
    }): Promise<void>;
}
//# sourceMappingURL=UpdateOrderItem.d.ts.map