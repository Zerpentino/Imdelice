import type { ITableRepository } from '../../domain/repositories/ITableRepository';
export declare class DeleteTable {
    private readonly repo;
    constructor(repo: ITableRepository);
    exec(id: number, hard?: boolean): Promise<void>;
}
//# sourceMappingURL=DeleteTable.d.ts.map