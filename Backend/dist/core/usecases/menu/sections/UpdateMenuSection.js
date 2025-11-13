"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateMenuSection = void 0;
class UpdateMenuSection {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id, data) {
        return this.repo.updateSection(id, data);
    }
}
exports.UpdateMenuSection = UpdateMenuSection;
//# sourceMappingURL=UpdateMenuSection.js.map