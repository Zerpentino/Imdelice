import type { IOrderRepository, PaymentsReportFilters, PaymentsReportResult } from "../../domain/repositories/IOrderRepository";
export declare class GetPaymentsReport {
    private readonly repo;
    constructor(repo: IOrderRepository);
    exec(filters: PaymentsReportFilters): Promise<PaymentsReportResult>;
}
//# sourceMappingURL=GetPaymentsReport.d.ts.map