import { Request, Response } from 'express';
import { CreateTable } from '../../core/usecases/tables/CreateTable';
import { ListTables } from '../../core/usecases/tables/ListTables';
import { GetTable } from '../../core/usecases/tables/GetTable';
import { UpdateTable } from '../../core/usecases/tables/UpdateTable';
import { DeleteTable } from '../../core/usecases/tables/DeleteTable';
export declare class TablesController {
    private readonly createTableUC;
    private readonly listTablesUC;
    private readonly getTableUC;
    private readonly updateTableUC;
    private readonly deleteTableUC;
    constructor(createTableUC: CreateTable, listTablesUC: ListTables, getTableUC: GetTable, updateTableUC: UpdateTable, deleteTableUC: DeleteTable);
    create: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    list: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    getOne: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    update: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    remove: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
}
//# sourceMappingURL=TablesController.d.ts.map