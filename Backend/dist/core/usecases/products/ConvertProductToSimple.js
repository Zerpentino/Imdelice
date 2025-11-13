"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ConvertProductToSimple = void 0;
class ConvertProductToSimple {
    constructor(repo) {
        this.repo = repo;
    }
    exec(productId, priceCents) {
        return this.repo.convertToSimple(productId, priceCents);
    }
}
exports.ConvertProductToSimple = ConvertProductToSimple;
//# sourceMappingURL=ConvertProductToSimple.js.map