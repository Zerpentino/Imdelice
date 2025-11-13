"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListRoles = void 0;
class ListRoles {
    constructor(repo) {
        this.repo = repo;
    }
    execute() {
        return this.repo.list();
    }
}
exports.ListRoles = ListRoles;
//# sourceMappingURL=ListRoles.js.map