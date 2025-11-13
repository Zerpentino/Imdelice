"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.CreateModifierGroupWithOptions = void 0;
class CreateModifierGroupWithOptions {
    constructor(repo) {
        this.repo = repo;
    }
    exec(input) {
        return this.repo.createGroupWithOptions(input);
    }
}
exports.CreateModifierGroupWithOptions = CreateModifierGroupWithOptions;
//# sourceMappingURL=CreateModifierGroupWithOptions.js.map