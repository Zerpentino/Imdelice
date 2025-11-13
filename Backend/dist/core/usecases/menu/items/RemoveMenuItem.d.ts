import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';
export declare class RemoveMenuItem {
    private readonly repo;
    constructor(repo: IMenuRepository);
    exec(id: number, hard?: boolean): Promise<void>;
}
//# sourceMappingURL=RemoveMenuItem.d.ts.map