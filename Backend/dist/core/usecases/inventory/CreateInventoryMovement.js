"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.CreateInventoryMovement = void 0;
class CreateInventoryMovement {
    constructor(repo) {
        this.repo = repo;
    }
    exec(input) {
        return this.repo.createMovement(input);
    }
}
exports.CreateInventoryMovement = CreateInventoryMovement;
//# sourceMappingURL=CreateInventoryMovement.js.map