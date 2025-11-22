import type { Response } from "express";
import type { AuthRequest } from "../middlewares/authenticate";
import { CreateExpense } from "../../core/usecases/expenses/CreateExpense";
import { ListExpenses } from "../../core/usecases/expenses/ListExpenses";
import { GetExpense } from "../../core/usecases/expenses/GetExpense";
import { UpdateExpense } from "../../core/usecases/expenses/UpdateExpense";
import { DeleteExpense } from "../../core/usecases/expenses/DeleteExpense";
import { GetExpensesSummary } from "../../core/usecases/expenses/GetExpensesSummary";
import { CreateInventoryMovement } from "../../core/usecases/inventory/CreateInventoryMovement";
import { CreateInventoryMovementByBarcode } from "../../core/usecases/inventory/CreateInventoryMovementByBarcode";
export declare class ExpensesController {
    private listUC;
    private getUC;
    private createUC;
    private updateUC;
    private deleteUC;
    private summaryUC;
    private createInventoryMovementUC;
    private createMovementByBarcodeUC;
    constructor(listUC: ListExpenses, getUC: GetExpense, createUC: CreateExpense, updateUC: UpdateExpense, deleteUC: DeleteExpense, summaryUC: GetExpensesSummary, createInventoryMovementUC: CreateInventoryMovement, createMovementByBarcodeUC: CreateInventoryMovementByBarcode);
    private parseDate;
    list: (req: AuthRequest, res: Response) => Promise<Response<any, Record<string, any>>>;
    get: (req: AuthRequest, res: Response) => Promise<Response<any, Record<string, any>>>;
    create: (req: AuthRequest, res: Response) => Promise<Response<any, Record<string, any>>>;
    update: (req: AuthRequest, res: Response) => Promise<Response<any, Record<string, any>>>;
    remove: (req: AuthRequest, res: Response) => Promise<Response<any, Record<string, any>>>;
    summary: (req: AuthRequest, res: Response) => Promise<Response<any, Record<string, any>>>;
    private createInventoryRecord;
}
//# sourceMappingURL=ExpensesController.d.ts.map