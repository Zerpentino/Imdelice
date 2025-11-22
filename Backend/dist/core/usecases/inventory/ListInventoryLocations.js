"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListInventoryLocations = void 0;
class ListInventoryLocations {
    constructor(repo) {
        this.repo = repo;
    }
    exec() {
        return this.repo.listLocations();
    }
}
exports.ListInventoryLocations = ListInventoryLocations;
//# sourceMappingURL=ListInventoryLocations.js.map