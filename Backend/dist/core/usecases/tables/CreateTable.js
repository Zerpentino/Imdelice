"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.CreateTable = void 0;
class CreateTable {
    constructor(repo) {
        this.repo = repo;
    }
    exec(input) {
        return this.repo.create(input);
    }
}
exports.CreateTable = CreateTable;
//# sourceMappingURL=CreateTable.js.map