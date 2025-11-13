"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.RemoveMenuItem = void 0;
class RemoveMenuItem {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id, hard = false) {
        return this.repo.removeItem(id, hard);
    }
}
exports.RemoveMenuItem = RemoveMenuItem;
//# sourceMappingURL=RemoveMenuItem.js.map