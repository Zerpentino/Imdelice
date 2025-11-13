"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListCategories = void 0;
class ListCategories {
    constructor(repo) {
        this.repo = repo;
    }
    exec() { return this.repo.list(); }
}
exports.ListCategories = ListCategories;
//# sourceMappingURL=ListCategories.js.map