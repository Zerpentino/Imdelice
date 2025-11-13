"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.RefundOrder = void 0;
class RefundOrder {
    constructor(repo) {
        this.repo = repo;
    }
    exec(orderId, input) {
        return this.repo.refundOrder(orderId, input);
    }
}
exports.RefundOrder = RefundOrder;
//# sourceMappingURL=RefundOrder.js.map