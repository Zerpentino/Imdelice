"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListKDS = void 0;
class ListKDS {
    constructor(repo) {
        this.repo = repo;
    }
    exec(filters) { return this.repo.listKDS(filters); }
}
exports.ListKDS = ListKDS;
//# sourceMappingURL=ListKDS.js.map