import type { ITableRepository } from '../../domain/repositories/ITableRepository';
type CreateTableInput = {
    name: string;
    seats?: number | null;
    isActive?: boolean;
};
export declare class CreateTable {
    private readonly repo;
    constructor(repo: ITableRepository);
    exec(input: CreateTableInput): Promise<{
        name: string;
        id: number;
        isActive: boolean;
        seats: number | null;
    }>;
}
export {};
//# sourceMappingURL=CreateTable.d.ts.map