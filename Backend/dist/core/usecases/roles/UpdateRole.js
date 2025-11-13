"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateRole = void 0;
class UpdateRole {
    constructor(repo) {
        this.repo = repo;
    }
    async execute(input) {
        // si cambia el nombre, valida duplicado
        if (input.name) {
            const existing = await this.repo.getByName(input.name);
            if (existing && existing.id !== input.id) {
                const e = new Error("El nombre del rol ya existe");
                e.status = 409;
                throw e;
            }
        }
        return this.repo.update(input);
    }
}
exports.UpdateRole = UpdateRole;
//# sourceMappingURL=UpdateRole.js.map