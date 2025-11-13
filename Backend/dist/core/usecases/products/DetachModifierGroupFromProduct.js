"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.DetachModifierGroupFromProduct = void 0;
class DetachModifierGroupFromProduct {
    constructor(repo) {
        this.repo = repo;
    }
    // puedes pasar linkId o (productId, groupId)
    exec(linkIdOr) {
        return this.repo.detachModifierGroup(linkIdOr);
    }
}
exports.DetachModifierGroupFromProduct = DetachModifierGroupFromProduct;
//# sourceMappingURL=DetachModifierGroupFromProduct.js.map