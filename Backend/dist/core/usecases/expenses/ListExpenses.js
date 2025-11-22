"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListExpenses = void 0;
class ListExpenses {
    constructor(repo) {
        this.repo = repo;
    }
    exec(filters) {
        return this.repo.list(filters);
    }
}
exports.ListExpenses = ListExpenses;
//# sourceMappingURL=ListExpenses.js.map