"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.SetChannelConfig = void 0;
class SetChannelConfig {
    constructor(repo) {
        this.repo = repo;
    }
    exec(source, markupPct) {
        return this.repo.upsert(source, markupPct);
    }
}
exports.SetChannelConfig = SetChannelConfig;
//# sourceMappingURL=SetChannelConfig.js.map