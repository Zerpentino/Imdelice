"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateMenuItem = void 0;
class UpdateMenuItem {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id, data) {
        return this.repo.updateItem(id, data);
    }
}
exports.UpdateMenuItem = UpdateMenuItem;
//# sourceMappingURL=UpdateMenuItem.js.map