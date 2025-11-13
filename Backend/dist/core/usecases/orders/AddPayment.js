"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.AddPayment = void 0;
class AddPayment {
    constructor(repo) {
        this.repo = repo;
    }
    exec(orderId, data) {
        return this.repo.addPayment(orderId, data);
    }
}
exports.AddPayment = AddPayment;
//# sourceMappingURL=AddPayment.js.map