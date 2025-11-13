"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListOrders = void 0;
class ListOrders {
    constructor(repo) {
        this.repo = repo;
    }
    exec(filters) {
        return this.repo.list(filters);
    }
}
exports.ListOrders = ListOrders;
//# sourceMappingURL=ListOrders.js.map