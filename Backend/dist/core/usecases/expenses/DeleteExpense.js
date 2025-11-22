"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.DeleteExpense = void 0;
class DeleteExpense {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id) {
        return this.repo.delete(id);
    }
}
exports.DeleteExpense = DeleteExpense;
//# sourceMappingURL=DeleteExpense.js.map