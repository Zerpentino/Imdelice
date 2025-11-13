"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateVariantModifierGroup = void 0;
class UpdateVariantModifierGroup {
    constructor(repo) {
        this.repo = repo;
    }
    exec(variantId, groupId, data) {
        return this.repo.updateVariantModifierGroup(variantId, groupId, data);
    }
}
exports.UpdateVariantModifierGroup = UpdateVariantModifierGroup;
//# sourceMappingURL=UpdateVariantModifierGroup.js.map