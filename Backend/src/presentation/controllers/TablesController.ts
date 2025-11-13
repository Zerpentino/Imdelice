import { Request, Response } from 'express';
import { CreateTable } from '../../core/usecases/tables/CreateTable';
import { ListTables } from '../../core/usecases/tables/ListTables';
import { GetTable } from '../../core/usecases/tables/GetTable';
import { UpdateTable } from '../../core/usecases/tables/UpdateTable';
import { DeleteTable } from '../../core/usecases/tables/DeleteTable';
import { CreateTableDto, UpdateTableDto } from '../dtos/tables.dto';
import { success, fail } from '../utils/apiResponse';

export class TablesController {
  constructor(
    private readonly createTableUC: CreateTable,
    private readonly listTablesUC: ListTables,
    private readonly getTableUC: GetTable,
    private readonly updateTableUC: UpdateTable,
    private readonly deleteTableUC: DeleteTable
  ) {}

  create = async (req: Request, res: Response) => {
    try {
      const dto = CreateTableDto.parse(req.body);
      const table = await this.createTableUC.exec(dto);
      return success(res, table, 'Created', 201);
    } catch (err: any) {
      return fail(res, err?.message || 'Error creating table', 400, err);
    }
  };

  list = async (req: Request, res: Response) => {
    try {
      const includeInactive = req.query.includeInactive === 'true';
      const tables = await this.listTablesUC.exec(includeInactive);
      return success(res, tables);
    } catch (err: any) {
      return fail(res, err?.message || 'Error listing tables', 400, err);
    }
  };

  getOne = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      const table = await this.getTableUC.exec(id);
      if (!table) return fail(res, 'Table not found', 404);
      return success(res, table);
    } catch (err: any) {
      return fail(res, err?.message || 'Error fetching table', 400, err);
    }
  };

  update = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      const dto = UpdateTableDto.parse(req.body);
      const table = await this.updateTableUC.exec(id, dto);
      return success(res, table, 'Updated');
    } catch (err: any) {
      return fail(res, err?.message || 'Error updating table', 400, err);
    }
  };

  remove = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      const hard = req.query.hard === 'true';
      await this.deleteTableUC.exec(id, hard);
      return success(res, null, hard ? 'Deleted' : 'Deactivated', 204);
    } catch (err: any) {
      return fail(res, err?.message || 'Error deleting table', 400, err);
    }
  };
}
