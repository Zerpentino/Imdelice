"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.TablesController = void 0;
const tables_dto_1 = require("../dtos/tables.dto");
const apiResponse_1 = require("../utils/apiResponse");
class TablesController {
    constructor(createTableUC, listTablesUC, getTableUC, updateTableUC, deleteTableUC) {
        this.createTableUC = createTableUC;
        this.listTablesUC = listTablesUC;
        this.getTableUC = getTableUC;
        this.updateTableUC = updateTableUC;
        this.deleteTableUC = deleteTableUC;
        this.create = async (req, res) => {
            try {
                const dto = tables_dto_1.CreateTableDto.parse(req.body);
                const table = await this.createTableUC.exec(dto);
                return (0, apiResponse_1.success)(res, table, 'Created', 201);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || 'Error creating table', 400, err);
            }
        };
        this.list = async (req, res) => {
            try {
                const includeInactive = req.query.includeInactive === 'true';
                const tables = await this.listTablesUC.exec(includeInactive);
                return (0, apiResponse_1.success)(res, tables);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || 'Error listing tables', 400, err);
            }
        };
        this.getOne = async (req, res) => {
            try {
                const id = Number(req.params.id);
                const table = await this.getTableUC.exec(id);
                if (!table)
                    return (0, apiResponse_1.fail)(res, 'Table not found', 404);
                return (0, apiResponse_1.success)(res, table);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || 'Error fetching table', 400, err);
            }
        };
        this.update = async (req, res) => {
            try {
                const id = Number(req.params.id);
                const dto = tables_dto_1.UpdateTableDto.parse(req.body);
                const table = await this.updateTableUC.exec(id, dto);
                return (0, apiResponse_1.success)(res, table, 'Updated');
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || 'Error updating table', 400, err);
            }
        };
        this.remove = async (req, res) => {
            try {
                const id = Number(req.params.id);
                const hard = req.query.hard === 'true';
                await this.deleteTableUC.exec(id, hard);
                return (0, apiResponse_1.success)(res, null, hard ? 'Deleted' : 'Deactivated', 204);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || 'Error deleting table', 400, err);
            }
        };
    }
}
exports.TablesController = TablesController;
//# sourceMappingURL=TablesController.js.map