"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.GetModifierGroup = void 0;
class GetModifierGroup {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id) { return this.repo.getGroup(id); }
}
exports.GetModifierGroup = GetModifierGroup;
//# sourceMappingURL=GetModifierGroup.js.map