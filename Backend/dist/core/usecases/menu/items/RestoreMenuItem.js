"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.RestoreMenuItem = void 0;
class RestoreMenuItem {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id) {
        return this.repo.restoreItem(id);
    }
}
exports.RestoreMenuItem = RestoreMenuItem;
//# sourceMappingURL=RestoreMenuItem.js.map