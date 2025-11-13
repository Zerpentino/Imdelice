"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.CreateOrder = void 0;
class CreateOrder {
    constructor(repo) {
        this.repo = repo;
    }
    exec(input, servedByUserId) {
        return this.repo.create(input, servedByUserId);
    }
}
exports.CreateOrder = CreateOrder;
//# sourceMappingURL=CreateOrder.js.map