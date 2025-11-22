"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListInventoryMovements = void 0;
class ListInventoryMovements {
    constructor(repo) {
        this.repo = repo;
    }
    exec(filter) {
        return this.repo.listMovements(filter);
    }
}
exports.ListInventoryMovements = ListInventoryMovements;
//# sourceMappingURL=ListInventoryMovements.js.map