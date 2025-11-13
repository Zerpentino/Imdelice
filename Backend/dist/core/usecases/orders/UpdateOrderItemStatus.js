"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateOrderItemStatus = void 0;
class UpdateOrderItemStatus {
    constructor(repo) {
        this.repo = repo;
    }
    exec(orderItemId, status) {
        return this.repo.updateItemStatus(orderItemId, status);
    }
}
exports.UpdateOrderItemStatus = UpdateOrderItemStatus;
//# sourceMappingURL=UpdateOrderItemStatus.js.map