"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.AddComboItems = void 0;
class AddComboItems {
    constructor(repo) {
        this.repo = repo;
    }
    exec(comboProductId, items) {
        return this.repo.addComboItems(comboProductId, items);
    }
}
exports.AddComboItems = AddComboItems;
//# sourceMappingURL=AddComboItems.js.map