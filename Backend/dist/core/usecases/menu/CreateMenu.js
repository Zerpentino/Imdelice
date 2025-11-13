"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.CreateMenu = void 0;
class CreateMenu {
    constructor(repo) {
        this.repo = repo;
    }
    exec(input) {
        return this.repo.createMenu(input);
    }
}
exports.CreateMenu = CreateMenu;
//# sourceMappingURL=CreateMenu.js.map