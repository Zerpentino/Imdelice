"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateModifierGroupPosition = void 0;
class UpdateModifierGroupPosition {
    constructor(repo) {
        this.repo = repo;
    }
    exec(linkId, position) {
        return this.repo.updateModifierGroupPosition(linkId, position);
    }
}
exports.UpdateModifierGroupPosition = UpdateModifierGroupPosition;
//# sourceMappingURL=UpdateModifierGroupPosition.js.map