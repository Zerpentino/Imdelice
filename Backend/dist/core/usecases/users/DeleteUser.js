"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.DeleteUser = void 0;
class DeleteUser {
    constructor(repo) {
        this.repo = repo;
    }
    async execute(id) {
        await this.repo.delete(id);
    }
}
exports.DeleteUser = DeleteUser;
//# sourceMappingURL=DeleteUser.js.map