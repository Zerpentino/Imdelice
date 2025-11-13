"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.DeleteRole = void 0;
class DeleteRole {
    constructor(repo) {
        this.repo = repo;
    }
    async execute(id) {
        await this.repo.delete(id);
    }
}
exports.DeleteRole = DeleteRole;
//# sourceMappingURL=DeleteRole.js.map