import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';
export declare class ListArchivedMenuItems {
    private readonly repo;
    constructor(repo: IMenuRepository);
    exec(sectionId: number): Promise<import("../../../domain/repositories/IMenuRepository").MenuItemWithRef[]>;
}
//# sourceMappingURL=ListArchivedMenuItems.d.ts.map