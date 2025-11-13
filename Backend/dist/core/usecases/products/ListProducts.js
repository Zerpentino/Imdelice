"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListProducts = void 0;
class ListProducts {
    constructor(repo) {
        this.repo = repo;
    }
    exec(filter) {
        return this.repo.list(filter);
    }
}
exports.ListProducts = ListProducts;
//# sourceMappingURL=ListProducts.js.map