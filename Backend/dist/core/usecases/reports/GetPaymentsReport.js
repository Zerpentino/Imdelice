"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.GetPaymentsReport = void 0;
class GetPaymentsReport {
    constructor(repo) {
        this.repo = repo;
    }
    exec(filters) {
        return this.repo.getPaymentsReport(filters);
    }
}
exports.GetPaymentsReport = GetPaymentsReport;
//# sourceMappingURL=GetPaymentsReport.js.map