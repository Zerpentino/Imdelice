import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';
export declare class DeleteMenuItemHard {
    private readonly repo;
    constructor(repo: IMenuRepository);
    exec(id: number): Promise<void>;
}
//# sourceMappingURL=DeleteMenuItemHard.d.ts.map