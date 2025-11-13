"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListUsers = void 0;
class ListUsers {
    constructor(repo) {
        this.repo = repo;
    }
    async execute() {
        return this.repo.list();
    }
}
exports.ListUsers = ListUsers;
//# sourceMappingURL=ListUsers.js.map