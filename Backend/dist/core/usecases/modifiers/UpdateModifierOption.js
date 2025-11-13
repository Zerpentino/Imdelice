"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateModifierOption = void 0;
class UpdateModifierOption {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id, data) {
        return this.repo.updateOption(id, data);
    }
}
exports.UpdateModifierOption = UpdateModifierOption;
//# sourceMappingURL=UpdateModifierOption.js.map