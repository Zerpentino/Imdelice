import type { Response } from "express";
import type { AuthRequest } from "../middlewares/authenticate";
import { GetPaymentsReport } from "../../core/usecases/reports/GetPaymentsReport";
export declare class ReportsController {
    private readonly paymentsReportUC;
    constructor(paymentsReportUC: GetPaymentsReport);
    payments: (req: AuthRequest, res: Response) => Promise<Response<any, Record<string, any>>>;
}
//# sourceMappingURL=ReportsController.d.ts.map