"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.GetTable = void 0;
class GetTable {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id) {
        return this.repo.getById(id);
    }
}
exports.GetTable = GetTable;
//# sourceMappingURL=GetTable.js.map