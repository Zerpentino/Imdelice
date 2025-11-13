"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ListChannelConfigs = void 0;
class ListChannelConfigs {
    constructor(repo) {
        this.repo = repo;
    }
    exec() {
        return this.repo.list();
    }
}
exports.ListChannelConfigs = ListChannelConfigs;
//# sourceMappingURL=ListChannelConfigs.js.map