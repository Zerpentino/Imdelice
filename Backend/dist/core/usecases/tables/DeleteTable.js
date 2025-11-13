"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.DeleteTable = void 0;
class DeleteTable {
    constructor(repo) {
        this.repo = repo;
    }
    async exec(id, hard = false) {
        if (hard) {
            await this.repo.deleteHard(id);
            return;
        }
        await this.repo.deactivate(id);
    }
}
exports.DeleteTable = DeleteTable;
//# sourceMappingURL=DeleteTable.js.map