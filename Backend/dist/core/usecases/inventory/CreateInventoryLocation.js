"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.CreateInventoryLocation = void 0;
class CreateInventoryLocation {
    constructor(repo) {
        this.repo = repo;
    }
    exec(input) {
        return this.repo.createLocation(input);
    }
}
exports.CreateInventoryLocation = CreateInventoryLocation;
//# sourceMappingURL=CreateInventoryLocation.js.map