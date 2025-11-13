"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateOrderStatus = void 0;
class UpdateOrderStatus {
    constructor(repo) {
        this.repo = repo;
    }
    exec(orderId, status) {
        return this.repo.updateStatus(orderId, status);
    }
}
exports.UpdateOrderStatus = UpdateOrderStatus;
//# sourceMappingURL=UpdateOrderStatus.js.map