"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.RemoveComboItem = void 0;
class RemoveComboItem {
    constructor(repo) {
        this.repo = repo;
    }
    exec(comboItemId) { return this.repo.removeComboItem(comboItemId); }
}
exports.RemoveComboItem = RemoveComboItem;
//# sourceMappingURL=RemoveComboItem.js.map