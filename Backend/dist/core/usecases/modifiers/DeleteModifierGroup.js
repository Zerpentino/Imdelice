"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.DeleteModifierGroup = void 0;
class DeleteModifierGroup {
    constructor(repo) {
        this.repo = repo;
    }
    async exec(id, hard = false) {
        return hard ? this.repo.deleteGroupHard(id) : this.repo.deactivateGroup(id);
    }
}
exports.DeleteModifierGroup = DeleteModifierGroup;
//# sourceMappingURL=DeleteModifierGroup.js.map