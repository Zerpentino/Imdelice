"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.SetRolePermissions = void 0;
class SetRolePermissions {
    constructor(repo) {
        this.repo = repo;
    }
    execute(roleId, codes) {
        return this.repo.replaceRolePermissions(roleId, codes);
    }
}
exports.SetRolePermissions = SetRolePermissions;
//# sourceMappingURL=SetRolePermissions.js.map