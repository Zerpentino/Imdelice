"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateComboItem = void 0;
class UpdateComboItem {
    constructor(repo) {
        this.repo = repo;
    }
    exec(comboItemId, data) {
        return this.repo.updateComboItem(comboItemId, data);
    }
}
exports.UpdateComboItem = UpdateComboItem;
//# sourceMappingURL=UpdateComboItem.js.map