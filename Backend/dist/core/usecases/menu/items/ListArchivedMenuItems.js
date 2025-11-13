"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListArchivedMenuItems = void 0;
class ListArchivedMenuItems {
    constructor(repo) {
        this.repo = repo;
    }
    exec(sectionId) {
        return this.repo.listArchivedItems(sectionId);
    }
}
exports.ListArchivedMenuItems = ListArchivedMenuItems;
//# sourceMappingURL=ListArchivedMenuItems.js.map