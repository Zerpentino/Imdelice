"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.GetExpensesSummary = void 0;
class GetExpensesSummary {
    constructor(repo) {
        this.repo = repo;
    }
    exec(filters) {
        return this.repo.getSummary(filters);
    }
}
exports.GetExpensesSummary = GetExpensesSummary;
//# sourceMappingURL=GetExpensesSummary.js.map