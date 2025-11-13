"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ReplaceProductVariants = void 0;
class ReplaceProductVariants {
    constructor(repo) {
        this.repo = repo;
    }
    exec(productId, variants) {
        return this.repo.replaceVariants(productId, variants);
    }
}
exports.ReplaceProductVariants = ReplaceProductVariants;
//# sourceMappingURL=ReplaceProductVariants.js.map