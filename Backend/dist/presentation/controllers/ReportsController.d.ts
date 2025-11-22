import type { Response } from "express";
import type { AuthRequest } from "../middlewares/authenticate";
import { GetPaymentsReport } from "../../core/usecases/reports/GetPaymentsReport";
import { GetProfitLossReport } from "../../core/usecases/reports/GetProfitLossReport";
export declare class ReportsController {
    private readonly paymentsReportUC;
    private readonly profitLossReportUC;
    constructor(paymentsReportUC: GetPaymentsReport, profitLossReportUC: GetProfitLossReport);
    payments: (req: AuthRequest, res: Response) => Promise<Response<any, Record<string, any>>>;
    profitLoss: (req: AuthRequest, res: Response) => Promise<Response<any, Record<string, any>>>;
}
//# sourceMappingURL=ReportsController.d.ts.map