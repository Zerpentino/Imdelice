"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.RestoreMenuSection = void 0;
class RestoreMenuSection {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id) {
        return this.repo.restoreSection(id);
    }
}
exports.RestoreMenuSection = RestoreMenuSection;
//# sourceMappingURL=RestoreMenuSection.js.map