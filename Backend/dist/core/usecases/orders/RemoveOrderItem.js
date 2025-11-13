"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.RemoveOrderItem = void 0;
class RemoveOrderItem {
    constructor(repo) {
        this.repo = repo;
    }
    exec(orderItemId) {
        return this.repo.removeItem(orderItemId);
    }
}
exports.RemoveOrderItem = RemoveOrderItem;
//# sourceMappingURL=RemoveOrderItem.js.map