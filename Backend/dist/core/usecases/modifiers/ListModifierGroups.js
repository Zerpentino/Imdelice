"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListModifierGroups = void 0;
class ListModifierGroups {
    constructor(repo) {
        this.repo = repo;
    }
    exec(filter) {
        return this.repo.listGroups(filter);
    }
}
exports.ListModifierGroups = ListModifierGroups;
//# sourceMappingURL=ListModifierGroups.js.map