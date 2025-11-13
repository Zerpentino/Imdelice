"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateOrderItem = void 0;
class UpdateOrderItem {
    constructor(repo) {
        this.repo = repo;
    }
    exec(orderItemId, data) {
        return this.repo.updateItem(orderItemId, data);
    }
}
exports.UpdateOrderItem = UpdateOrderItem;
//# sourceMappingURL=UpdateOrderItem.js.map