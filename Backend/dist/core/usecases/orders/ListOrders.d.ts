import type { IOrderRepository, ListOrdersFilters, ListOrdersItem } from "../../domain/repositories/IOrderRepository";
export declare class ListOrders {
    private readonly repo;
    constructor(repo: IOrderRepository);
    exec(filters: ListOrdersFilters): Promise<ListOrdersItem[]>;
}
//# sourceMappingURL=ListOrders.d.ts.map