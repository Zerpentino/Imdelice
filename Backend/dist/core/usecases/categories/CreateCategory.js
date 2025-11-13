"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.CreateCategory = void 0;
class CreateCategory {
    constructor(repo) {
        this.repo = repo;
    }
    exec(input) {
        return this.repo.create(input);
    }
}
exports.CreateCategory = CreateCategory;
//# sourceMappingURL=CreateCategory.js.map