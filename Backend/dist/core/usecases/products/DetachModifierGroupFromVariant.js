"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.DetachModifierGroupFromVariant = void 0;
class DetachModifierGroupFromVariant {
    constructor(repo) {
        this.repo = repo;
    }
    exec(variantId, groupId) {
        return this.repo.detachModifierGroupFromVariant(variantId, groupId);
    }
}
exports.DetachModifierGroupFromVariant = DetachModifierGroupFromVariant;
//# sourceMappingURL=DetachModifierGroupFromVariant.js.map