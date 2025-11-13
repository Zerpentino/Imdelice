"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.GetMenuPublic = void 0;
class GetMenuPublic {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id) {
        return this.repo.getMenuPublic(id);
    }
}
exports.GetMenuPublic = GetMenuPublic;
//# sourceMappingURL=GetMenuPublic.js.map