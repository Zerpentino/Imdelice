"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListProductsByModifierGroup = void 0;
class ListProductsByModifierGroup {
    constructor(repo) {
        this.repo = repo;
    }
    exec(groupId, filter) {
        return this.repo.listProductsByGroup(groupId, filter);
    }
}
exports.ListProductsByModifierGroup = ListProductsByModifierGroup;
//# sourceMappingURL=ListProductsByModifierGroup.js.map