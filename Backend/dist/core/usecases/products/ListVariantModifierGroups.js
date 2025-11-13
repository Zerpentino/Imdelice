"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListVariantModifierGroups = void 0;
class ListVariantModifierGroups {
    constructor(repo) {
        this.repo = repo;
    }
    exec(variantId) {
        return this.repo.listVariantModifierGroups(variantId);
    }
}
exports.ListVariantModifierGroups = ListVariantModifierGroups;
//# sourceMappingURL=ListVariantModifierGroups.js.map