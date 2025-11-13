"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.GetUserById = void 0;
class GetUserById {
    constructor(repo) {
        this.repo = repo;
    }
    execute(id) { return this.repo.getById(id); }
}
exports.GetUserById = GetUserById;
//# sourceMappingURL=GetUserById.js.map