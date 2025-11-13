"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.AddOrderItem = void 0;
class AddOrderItem {
    constructor(repo) {
        this.repo = repo;
    }
    exec(orderId, data) {
        return this.repo.addItem(orderId, data);
    }
}
exports.AddOrderItem = AddOrderItem;
//# sourceMappingURL=AddOrderItem.js.map