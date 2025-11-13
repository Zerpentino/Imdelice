"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.AddMenuItem = void 0;
class AddMenuItem {
    constructor(repo) {
        this.repo = repo;
    }
    exec(input) {
        return this.repo.addItem(input);
    }
}
exports.AddMenuItem = AddMenuItem;
//# sourceMappingURL=AddMenuItem.js.map