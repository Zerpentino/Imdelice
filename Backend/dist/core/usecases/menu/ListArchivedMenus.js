"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListArchivedMenus = void 0;
class ListArchivedMenus {
    constructor(repo) {
        this.repo = repo;
    }
    exec() {
        return this.repo.listArchivedMenus();
    }
}
exports.ListArchivedMenus = ListArchivedMenus;
//# sourceMappingURL=ListArchivedMenus.js.map