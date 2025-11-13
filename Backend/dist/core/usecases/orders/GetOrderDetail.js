"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.GetOrderDetail = void 0;
class GetOrderDetail {
    constructor(repo) {
        this.repo = repo;
    }
    exec(orderId) { return this.repo.getById(orderId); }
}
exports.GetOrderDetail = GetOrderDetail;
//# sourceMappingURL=GetOrderDetail.js.map