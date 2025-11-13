import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';
export declare class DeleteMenuSection {
    private readonly repo;
    constructor(repo: IMenuRepository);
    exec(id: number, hard?: boolean): Promise<void>;
}
//# sourceMappingURL=DeleteMenuSection.d.ts.map