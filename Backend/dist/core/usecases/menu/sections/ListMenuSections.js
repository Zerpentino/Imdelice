"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListMenuSections = void 0;
class ListMenuSections {
    constructor(repo) {
        this.repo = repo;
    }
    exec(menuId) {
        return this.repo.listSections(menuId);
    }
}
exports.ListMenuSections = ListMenuSections;
//# sourceMappingURL=ListMenuSections.js.map