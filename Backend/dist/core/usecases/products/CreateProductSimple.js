"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.CreateProductSimple = void 0;
class CreateProductSimple {
    constructor(repo) {
        this.repo = repo;
    }
    exec(input) {
        return this.repo.createSimple(input);
    }
}
exports.CreateProductSimple = CreateProductSimple;
//# sourceMappingURL=CreateProductSimple.js.map