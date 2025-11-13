"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.DeleteMenuSectionHard = void 0;
class DeleteMenuSectionHard {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id) {
        return this.repo.deleteSectionHard(id);
    }
}
exports.DeleteMenuSectionHard = DeleteMenuSectionHard;
//# sourceMappingURL=DeleteMenuSectionHard.js.map