"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.DeleteMenu = void 0;
class DeleteMenu {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id, hard = false) {
        return this.repo.deleteMenu(id, hard);
    }
}
exports.DeleteMenu = DeleteMenu;
//# sourceMappingURL=DeleteMenu.js.map