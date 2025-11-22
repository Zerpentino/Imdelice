"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateExpense = void 0;
class UpdateExpense {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id, input) {
        return this.repo.update(id, input);
    }
}
exports.UpdateExpense = UpdateExpense;
//# sourceMappingURL=UpdateExpense.js.map