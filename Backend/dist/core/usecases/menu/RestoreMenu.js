"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.RestoreMenu = void 0;
class RestoreMenu {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id) {
        return this.repo.restoreMenu(id);
    }
}
exports.RestoreMenu = RestoreMenu;
//# sourceMappingURL=RestoreMenu.js.map