"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListModifierGroupsByProduct = void 0;
class ListModifierGroupsByProduct {
    constructor(repo) {
        this.repo = repo;
    }
    exec(productId) { return this.repo.listByProduct(productId); }
}
exports.ListModifierGroupsByProduct = ListModifierGroupsByProduct;
//# sourceMappingURL=ListModifierGroupsByProduct.js.map