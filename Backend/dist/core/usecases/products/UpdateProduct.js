"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateProduct = void 0;
class UpdateProduct {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id, data) { return this.repo.update(id, data); }
}
exports.UpdateProduct = UpdateProduct;
//# sourceMappingURL=UpdateProduct.js.map