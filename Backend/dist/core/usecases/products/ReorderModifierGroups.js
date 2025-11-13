"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ReorderModifierGroups = void 0;
class ReorderModifierGroups {
    constructor(repo) {
        this.repo = repo;
    }
    exec(productId, items) {
        return this.repo.reorderModifierGroups(productId, items);
    }
}
exports.ReorderModifierGroups = ReorderModifierGroups;
//# sourceMappingURL=ReorderModifierGroups.js.map