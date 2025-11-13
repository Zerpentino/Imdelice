"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.DeleteMenuItemHard = void 0;
class DeleteMenuItemHard {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id) {
        return this.repo.deleteItemHard(id);
    }
}
exports.DeleteMenuItemHard = DeleteMenuItemHard;
//# sourceMappingURL=DeleteMenuItemHard.js.map