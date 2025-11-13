"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.DeleteProduct = void 0;
class DeleteProduct {
    constructor(repo) {
        this.repo = repo;
    }
    async exec(id, hard = false) {
        return hard ? this.repo.deleteHard(id) : this.repo.deactivate(id);
    }
}
exports.DeleteProduct = DeleteProduct;
//# sourceMappingURL=DeleteProduct.js.map