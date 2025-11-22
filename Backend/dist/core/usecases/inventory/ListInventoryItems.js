"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListInventoryItems = void 0;
class ListInventoryItems {
    constructor(repo) {
        this.repo = repo;
    }
    exec(filter) {
        return this.repo.listItems(filter);
    }
}
exports.ListInventoryItems = ListInventoryItems;
//# sourceMappingURL=ListInventoryItems.js.map