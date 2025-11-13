import type { IMenuRepository } from '../../domain/repositories/IMenuRepository';
export declare class DeleteMenu {
    private readonly repo;
    constructor(repo: IMenuRepository);
    exec(id: number, hard?: boolean): Promise<void>;
}
//# sourceMappingURL=DeleteMenu.d.ts.map