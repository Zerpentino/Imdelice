import type { ITableRepository } from '../../domain/repositories/ITableRepository';
export declare class GetTable {
    private readonly repo;
    constructor(repo: ITableRepository);
    exec(id: number): Promise<{
        name: string;
        id: number;
        isActive: boolean;
        seats: number | null;
    } | null>;
}
//# sourceMappingURL=GetTable.d.ts.map