"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.CreateExpense = void 0;
class CreateExpense {
    constructor(repo) {
        this.repo = repo;
    }
    exec(input) {
        return this.repo.create(input);
    }
}
exports.CreateExpense = CreateExpense;
//# sourceMappingURL=CreateExpense.js.map