"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateCategory = void 0;
class UpdateCategory {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id, data) { return this.repo.update(id, data); }
}
exports.UpdateCategory = UpdateCategory;
//# sourceMappingURL=UpdateCategory.js.map