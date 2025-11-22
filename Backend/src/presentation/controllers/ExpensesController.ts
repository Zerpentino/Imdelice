import type { Response } from "express";
import type { AuthRequest } from "../middlewares/authenticate";
import { success, fail } from "../utils/apiResponse";
import {
  CreateExpenseDto,
  UpdateExpenseDto,
  ExpenseQueryDto,
  ExpensesSummaryQueryDto,
} from "../dtos/expenses.dto";
import { CreateExpense } from "../../core/usecases/expenses/CreateExpense";
import { ListExpenses } from "../../core/usecases/expenses/ListExpenses";
import { GetExpense } from "../../core/usecases/expenses/GetExpense";
import { UpdateExpense } from "../../core/usecases/expenses/UpdateExpense";
import { DeleteExpense } from "../../core/usecases/expenses/DeleteExpense";
import { GetExpensesSummary } from "../../core/usecases/expenses/GetExpensesSummary";
import { CreateInventoryMovement } from "../../core/usecases/inventory/CreateInventoryMovement";
import { CreateInventoryMovementByBarcode } from "../../core/usecases/inventory/CreateInventoryMovementByBarcode";

export class ExpensesController {
  constructor(
    private listUC: ListExpenses,
    private getUC: GetExpense,
    private createUC: CreateExpense,
    private updateUC: UpdateExpense,
    private deleteUC: DeleteExpense,
    private summaryUC: GetExpensesSummary,
    private createInventoryMovementUC: CreateInventoryMovement,
    private createMovementByBarcodeUC: CreateInventoryMovementByBarcode,
  ) {}

  private parseDate(value?: string, endOfDay = false, tzOffsetMinutes = 0) {
    if (!value) return undefined;

    const dateOnlyRegex = /^\d{4}-\d{2}-\d{2}$/;
    const localDateTimeRegex = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d{1,3})?)?$/;

    const normalizeLocal = (datePart: string, timePart?: string) => {
      const [yearStr, monthStr, dayStr] = datePart.split("-");
      const year = Number(yearStr);
      const month = Number(monthStr);
      const day = Number(dayStr);
      if (!year || !month || !day) throw new Error(`Fecha inválida: ${value}`);

      let hours = 0;
      let minutes = 0;
      let seconds = 0;
      let millis = 0;

      if (timePart) {
        const match = timePart.match(/^(\d{2}):(\d{2})(?::(\d{2})(?:\.(\d{1,3}))?)?$/);
        if (!match) throw new Error(`Hora inválida: ${value}`);
        hours = Number(match[1]);
        minutes = Number(match[2]);
        seconds = match[3] ? Number(match[3]) : 0;
        millis = match[4] ? Number(match[4].padEnd(3, "0")) : 0;
      } else if (endOfDay) {
        hours = 23;
        minutes = 59;
        seconds = 59;
        millis = 999;
      }

      const utcMillis = Date.UTC(year, month - 1, day, hours, minutes, seconds, millis);
      return new Date(utcMillis - tzOffsetMinutes * 60_000);
    };

    if (dateOnlyRegex.test(value)) {
      return normalizeLocal(value);
    }

    if (localDateTimeRegex.test(value)) {
      const [datePart, timePart] = value.split("T");
      return normalizeLocal(datePart, timePart);
    }

    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) {
      throw new Error(`Fecha inválida: ${value}`);
    }
    return parsed;
  }

  list = async (req: AuthRequest, res: Response) => {
    try {
      const filters = ExpenseQueryDto.parse(req.query);
      let tzOffsetMinutes = filters.tzOffsetMinutes ?? 0;
      if (tzOffsetMinutes < -720 || tzOffsetMinutes > 840) {
        throw new Error("tzOffsetMinutes inválido");
      }
      const data = await this.listUC.exec({
        ...filters,
        from: this.parseDate(filters.from, false, tzOffsetMinutes),
        to: this.parseDate(filters.to, true, tzOffsetMinutes),
      });
      return success(res, data);
    } catch (err: any) {
      return fail(res, err?.message || "Error listing expenses", 400, err);
    }
  };

  get = async (req: AuthRequest, res: Response) => {
    try {
      const id = Number(req.params.id);
      if (!Number.isFinite(id) || id <= 0) return fail(res, "Id inválido", 400);
      const expense = await this.getUC.exec(id);
      if (!expense) return fail(res, "Not found", 404);
      return success(res, expense);
    } catch (err: any) {
      return fail(res, err?.message || "Error fetching expense", 400, err);
    }
  };

  create = async (req: AuthRequest, res: Response) => {
    try {
      const dto = CreateExpenseDto.parse(req.body);
      const recordedByUserId = req.auth?.userId;
      if (!recordedByUserId) return fail(res, "Unauthorized", 401);
      const inventoryMovementId = dto.inventoryMovement
        ? await this.createInventoryRecord(dto.inventoryMovement, recordedByUserId)
        : undefined;

      const expense = await this.createUC.exec({
        concept: dto.concept,
        category: dto.category,
        amountCents: dto.amountCents,
        paymentMethod: dto.paymentMethod ?? null,
        notes: dto.notes ?? null,
        incurredAt: dto.incurredAt ? new Date(dto.incurredAt) : new Date(),
        recordedByUserId,
        inventoryMovementId: inventoryMovementId ?? null,
        metadata: dto.metadata ?? null,
      });
      return success(res, expense, "Created", 201);
    } catch (err: any) {
      return fail(res, err?.message || "Error creating expense", 400, err);
    }
  };

  update = async (req: AuthRequest, res: Response) => {
    try {
      const id = Number(req.params.id);
      if (!Number.isFinite(id) || id <= 0) return fail(res, "Id inválido", 400);
      const dto = UpdateExpenseDto.parse(req.body);
      const expense = await this.updateUC.exec(id, {
        ...dto,
        incurredAt: dto.incurredAt ? new Date(dto.incurredAt) : undefined,
      });
      return success(res, expense, "Updated");
    } catch (err: any) {
      return fail(res, err?.message || "Error updating expense", 400, err);
    }
  };

  remove = async (req: AuthRequest, res: Response) => {
    try {
      const id = Number(req.params.id);
      if (!Number.isFinite(id) || id <= 0) return fail(res, "Id inválido", 400);
      await this.deleteUC.exec(id);
      return success(res, null, "Deleted", 204);
    } catch (err: any) {
      return fail(res, err?.message || "Error deleting expense", 400, err);
    }
  };

  summary = async (req: AuthRequest, res: Response) => {
    try {
      const filters = ExpensesSummaryQueryDto.parse(req.query);
      let tzOffsetMinutes = filters.tzOffsetMinutes ?? 0;
      if (tzOffsetMinutes < -720 || tzOffsetMinutes > 840) {
        throw new Error("tzOffsetMinutes inválido");
      }
      const data = await this.summaryUC.exec({
        ...filters,
        from: this.parseDate(filters.from, false, tzOffsetMinutes),
        to: this.parseDate(filters.to, true, tzOffsetMinutes),
      });
      return success(res, data);
    } catch (err: any) {
      return fail(res, err?.message || "Error getting expenses summary", 400, err);
    }
  };

  private async createInventoryRecord(payload: any, userId: number) {
    if (payload.barcode) {
      const movement = await this.createMovementByBarcodeUC.exec({
        barcode: payload.barcode,
        locationId: payload.locationId,
        type: payload.type,
        quantity: payload.quantity,
        reason: payload.reason,
        userId,
      });
      return movement.id;
    }
    if (!payload.productId) {
      throw new Error("productId o barcode requerido para ajustar inventario");
    }
    const movement = await this.createInventoryMovementUC.exec({
      productId: payload.productId,
      variantId: payload.variantId ?? null,
      locationId: payload.locationId ?? null,
      type: payload.type,
      quantity: payload.quantity,
      reason: payload.reason,
      performedByUserId: userId,
    });
    return movement.id;
  }
}
