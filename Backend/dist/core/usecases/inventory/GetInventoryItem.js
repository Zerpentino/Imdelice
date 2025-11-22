"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.GetInventoryItem = void 0;
class GetInventoryItem {
    constructor(repo) {
        this.repo = repo;
    }
    exec(filter) {
        return this.repo.findItem(filter);
    }
}
exports.GetInventoryItem = GetInventoryItem;
//# sourceMappingURL=GetInventoryItem.js.map