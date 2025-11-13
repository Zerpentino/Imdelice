"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ReplaceModifierOptions = void 0;
class ReplaceModifierOptions {
    constructor(repo) {
        this.repo = repo;
    }
    exec(groupId, options) {
        return this.repo.replaceOptions(groupId, options);
    }
}
exports.ReplaceModifierOptions = ReplaceModifierOptions;
//# sourceMappingURL=ReplaceModifierOptions.js.map