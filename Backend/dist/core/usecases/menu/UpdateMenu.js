"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateMenu = void 0;
class UpdateMenu {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id, data) {
        return this.repo.updateMenu(id, data);
    }
}
exports.UpdateMenu = UpdateMenu;
//# sourceMappingURL=UpdateMenu.js.map