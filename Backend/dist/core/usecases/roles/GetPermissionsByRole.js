"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.GetPermissionsByRole = void 0;
class GetPermissionsByRole {
    constructor(repo) {
        this.repo = repo;
    }
    execute(roleId) {
        return this.repo.listByRole(roleId);
    }
}
exports.GetPermissionsByRole = GetPermissionsByRole;
//# sourceMappingURL=GetPermissionsByRole.js.map