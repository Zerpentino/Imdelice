import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';
export declare class RestoreMenuSection {
    private readonly repo;
    constructor(repo: IMenuRepository);
    exec(id: number): Promise<void>;
}
//# sourceMappingURL=RestoreMenuSection.d.ts.map