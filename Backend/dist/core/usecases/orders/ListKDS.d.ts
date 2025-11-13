import type { IOrderRepository, KDSFilters } from "../../domain/repositories/IOrderRepository";
export declare class ListKDS {
    private repo;
    constructor(repo: IOrderRepository);
    exec(filters: KDSFilters): Promise<import("../../domain/repositories/IOrderRepository").KDSOrderTicket[]>;
}
//# sourceMappingURL=ListKDS.d.ts.map