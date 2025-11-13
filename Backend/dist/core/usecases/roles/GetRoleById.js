"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.GetRoleById = void 0;
class GetRoleById {
    constructor(repo) {
        this.repo = repo;
    }
    execute(id) {
        return this.repo.getById(id);
    }
}
exports.GetRoleById = GetRoleById;
//# sourceMappingURL=GetRoleById.js.map