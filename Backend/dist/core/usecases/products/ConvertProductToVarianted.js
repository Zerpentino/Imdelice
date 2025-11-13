"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ConvertProductToVarianted = void 0;
class ConvertProductToVarianted {
    constructor(repo) {
        this.repo = repo;
    }
    exec(productId, variants) {
        return this.repo.convertToVarianted(productId, variants);
    }
}
exports.ConvertProductToVarianted = ConvertProductToVarianted;
//# sourceMappingURL=ConvertProductToVarianted.js.map