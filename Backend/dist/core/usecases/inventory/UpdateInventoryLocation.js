"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateInventoryLocation = void 0;
class UpdateInventoryLocation {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id, input) {
        return this.repo.updateLocation(id, input);
    }
}
exports.UpdateInventoryLocation = UpdateInventoryLocation;
//# sourceMappingURL=UpdateInventoryLocation.js.map