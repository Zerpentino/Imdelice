"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.GetExpense = void 0;
class GetExpense {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id) {
        return this.repo.getById(id);
    }
}
exports.GetExpense = GetExpense;
//# sourceMappingURL=GetExpense.js.map