"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.CreateMenuSection = void 0;
class CreateMenuSection {
    constructor(repo) {
        this.repo = repo;
    }
    exec(input) {
        return this.repo.createSection(input);
    }
}
exports.CreateMenuSection = CreateMenuSection;
//# sourceMappingURL=CreateMenuSection.js.map