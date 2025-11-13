"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.CreateRole = void 0;
class CreateRole {
    constructor(repo) {
        this.repo = repo;
    }
    async execute(input) {
        // nombre único
        const dup = await this.repo.getByName(input.name);
        if (dup) {
            const e = new Error("El nombre del rol ya existe");
            e.status = 409;
            throw e;
        }
        return this.repo.create(input);
    }
}
exports.CreateRole = CreateRole;
//# sourceMappingURL=CreateRole.js.map