import type { Table } from '@prisma/client';
export interface ITableRepository {
    create(data: {
        name: string;
        seats?: number | null;
        isActive?: boolean;
    }): Promise<Table>;
    list(options?: {
        includeInactive?: boolean;
    }): Promise<Table[]>;
    getById(id: number): Promise<Table | null>;
    update(id: number, data: Partial<Pick<Table, 'name' | 'seats' | 'isActive'>>): Promise<Table>;
    deactivate(id: number): Promise<void>;
    deleteHard(id: number): Promise<void>;
}
//# sourceMappingURL=ITableRepository.d.ts.map