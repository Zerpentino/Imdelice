import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';
export declare class DeleteMenuSectionHard {
    private readonly repo;
    constructor(repo: IMenuRepository);
    exec(id: number): Promise<void>;
}
//# sourceMappingURL=DeleteMenuSectionHard.d.ts.map