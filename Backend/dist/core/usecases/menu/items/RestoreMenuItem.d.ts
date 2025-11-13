import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';
export declare class RestoreMenuItem {
    private readonly repo;
    constructor(repo: IMenuRepository);
    exec(id: number): Promise<void>;
}
//# sourceMappingURL=RestoreMenuItem.d.ts.map