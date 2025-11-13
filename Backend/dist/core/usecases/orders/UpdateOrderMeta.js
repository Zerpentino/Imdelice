"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateOrderMeta = void 0;
class UpdateOrderMeta {
    constructor(repo) {
        this.repo = repo;
    }
    exec(orderId, data) {
        return this.repo.updateMeta(orderId, data);
    }
}
exports.UpdateOrderMeta = UpdateOrderMeta;
//# sourceMappingURL=UpdateOrderMeta.js.map