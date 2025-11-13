"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateTable = void 0;
class UpdateTable {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id, data) {
        return this.repo.update(id, data);
    }
}
exports.UpdateTable = UpdateTable;
//# sourceMappingURL=UpdateTable.js.map