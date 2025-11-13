"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.AttachModifierGroupToProduct = void 0;
class AttachModifierGroupToProduct {
    constructor(repo) {
        this.repo = repo;
    }
    exec(input) {
        return this.repo.attachModifierGroup(input.productId, input.groupId, input.position ?? 0);
    }
}
exports.AttachModifierGroupToProduct = AttachModifierGroupToProduct;
//# sourceMappingURL=AttachModifierGroupToProduct.js.map