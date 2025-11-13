"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.DeleteMenuSection = void 0;
class DeleteMenuSection {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id, hard = false) {
        return this.repo.deleteSection(id, hard);
    }
}
exports.DeleteMenuSection = DeleteMenuSection;
//# sourceMappingURL=DeleteMenuSection.js.map