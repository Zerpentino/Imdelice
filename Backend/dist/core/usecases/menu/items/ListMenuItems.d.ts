import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';
export declare class ListMenuItems {
    private readonly repo;
    constructor(repo: IMenuRepository);
    exec(sectionId: number): Promise<import("../../../domain/repositories/IMenuRepository").MenuItemWithRef[]>;
}
//# sourceMappingURL=ListMenuItems.d.ts.map