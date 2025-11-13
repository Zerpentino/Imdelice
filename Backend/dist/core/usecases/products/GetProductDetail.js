"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.GetProductDetail = void 0;
class GetProductDetail {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id) { return this.repo.getById(id); }
}
exports.GetProductDetail = GetProductDetail;
//# sourceMappingURL=GetProductDetail.js.map