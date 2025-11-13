"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.CreateProductVarianted = void 0;
class CreateProductVarianted {
    constructor(repo) {
        this.repo = repo;
    }
    exec(input) {
        return this.repo.createVarianted(input);
    }
}
exports.CreateProductVarianted = CreateProductVarianted;
//# sourceMappingURL=CreateProductVarianted.js.map