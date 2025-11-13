import type { ITableRepository } from '../../domain/repositories/ITableRepository';
import type { Table } from '@prisma/client';
type UpdateTableInput = Partial<Pick<Table, 'name' | 'seats' | 'isActive'>>;
export declare class UpdateTable {
    private readonly repo;
    constructor(repo: ITableRepository);
    exec(id: number, data: UpdateTableInput): Promise<{
        name: string;
        id: number;
        isActive: boolean;
        seats: number | null;
    }>;
}
export {};
//# sourceMappingURL=UpdateTable.d.ts.map