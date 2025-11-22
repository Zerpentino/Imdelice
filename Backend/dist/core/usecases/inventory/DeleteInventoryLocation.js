"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.DeleteInventoryLocation = void 0;
class DeleteInventoryLocation {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id) {
        return this.repo.deleteLocation(id);
    }
}
exports.DeleteInventoryLocation = DeleteInventoryLocation;
//# sourceMappingURL=DeleteInventoryLocation.js.map