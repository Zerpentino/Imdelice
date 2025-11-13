"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListTables = void 0;
class ListTables {
    constructor(repo) {
        this.repo = repo;
    }
    exec(includeInactive = false) {
        return this.repo.list({ includeInactive });
    }
}
exports.ListTables = ListTables;
//# sourceMappingURL=ListTables.js.map