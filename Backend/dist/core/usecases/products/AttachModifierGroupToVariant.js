"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.AttachModifierGroupToVariant = void 0;
class AttachModifierGroupToVariant {
    constructor(repo) {
        this.repo = repo;
    }
    exec(input) {
        return this.repo.attachModifierGroupToVariant(input.variantId, {
            groupId: input.groupId,
            minSelect: input.minSelect,
            maxSelect: input.maxSelect,
            isRequired: input.isRequired
        });
    }
}
exports.AttachModifierGroupToVariant = AttachModifierGroupToVariant;
//# sourceMappingURL=AttachModifierGroupToVariant.js.map