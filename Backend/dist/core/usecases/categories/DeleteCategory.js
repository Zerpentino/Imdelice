"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.DeleteCategory = void 0;
class DeleteCategory {
    constructor(repo) {
        this.repo = repo;
    }
    async exec(id, hard = false) {
        return hard ? this.repo.deleteHard(id) : this.repo.deactivate(id);
    }
}
exports.DeleteCategory = DeleteCategory;
//# sourceMappingURL=DeleteCategory.js.map