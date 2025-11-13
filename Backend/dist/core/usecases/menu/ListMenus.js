"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListMenus = void 0;
class ListMenus {
    constructor(repo) {
        this.repo = repo;
    }
    exec() {
        return this.repo.listMenus();
    }
}
exports.ListMenus = ListMenus;
//# sourceMappingURL=ListMenus.js.map