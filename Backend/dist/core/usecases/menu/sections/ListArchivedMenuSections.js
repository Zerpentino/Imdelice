"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListArchivedMenuSections = void 0;
class ListArchivedMenuSections {
    constructor(repo) {
        this.repo = repo;
    }
    exec(menuId) {
        return this.repo.listArchivedSections(menuId);
    }
}
exports.ListArchivedMenuSections = ListArchivedMenuSections;
//# sourceMappingURL=ListArchivedMenuSections.js.map