"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.CreateProductCombo = void 0;
class CreateProductCombo {
    constructor(repo) {
        this.repo = repo;
    }
    exec(input) {
        return this.repo.createCombo(input);
    }
}
exports.CreateProductCombo = CreateProductCombo;
//# sourceMappingURL=CreateProductCombo.js.map