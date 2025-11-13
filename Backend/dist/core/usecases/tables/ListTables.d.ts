import type { ITableRepository } from '../../domain/repositories/ITableRepository';
export declare class ListTables {
    private readonly repo;
    constructor(repo: ITableRepository);
    exec(includeInactive?: boolean): Promise<{
        name: string;
        id: number;
        isActive: boolean;
        seats: number | null;
    }[]>;
}
//# sourceMappingURL=ListTables.d.ts.map