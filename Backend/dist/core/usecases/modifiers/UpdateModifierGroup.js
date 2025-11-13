"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateModifierGroup = void 0;
class UpdateModifierGroup {
    constructor(repo) {
        this.repo = repo;
    }
    exec(id, data) {
        const { replaceOptions, ...groupData } = data; // ⬅️ filtramos aquí
        const normalized = {
            ...groupData,
            appliesToCategoryId: data.appliesToCategoryId ?? data.categoryId ?? groupData.appliesToCategoryId,
        };
        return this.repo.updateGroup(id, normalized);
    }
}
exports.UpdateModifierGroup = UpdateModifierGroup;
//# sourceMappingURL=UpdateModifierGroup.js.map