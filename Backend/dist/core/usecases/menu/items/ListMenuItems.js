"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListMenuItems = void 0;
class ListMenuItems {
    constructor(repo) {
        this.repo = repo;
    }
    exec(sectionId) {
        return this.repo.listItems(sectionId);
    }
}
exports.ListMenuItems = ListMenuItems;
//# sourceMappingURL=ListMenuItems.js.map