import type { IMenuRepository } from '../../domain/repositories/IMenuRepository';
export declare class RestoreMenu {
    private readonly repo;
    constructor(repo: IMenuRepository);
    exec(id: number): Promise<void>;
}
//# sourceMappingURL=RestoreMenu.d.ts.map