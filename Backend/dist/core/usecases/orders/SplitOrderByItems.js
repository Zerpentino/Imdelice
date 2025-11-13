"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.SplitOrderByItems = void 0;
class SplitOrderByItems {
    constructor(repo) {
        this.repo = repo;
    }
    exec(sourceOrderId, input) {
        return this.repo.splitOrderByItems(sourceOrderId, input);
    }
}
exports.SplitOrderByItems = SplitOrderByItems;
//# sourceMappingURL=SplitOrderByItems.js.map