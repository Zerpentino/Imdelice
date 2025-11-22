"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ExpensesController = void 0;
const apiResponse_1 = require("../utils/apiResponse");
const expenses_dto_1 = require("../dtos/expenses.dto");
class ExpensesController {
    constructor(listUC, getUC, createUC, updateUC, deleteUC, summaryUC, createInventoryMovementUC, createMovementByBarcodeUC) {
        this.listUC = listUC;
        this.getUC = getUC;
        this.createUC = createUC;
        this.updateUC = updateUC;
        this.deleteUC = deleteUC;
        this.summaryUC = summaryUC;
        this.createInventoryMovementUC = createInventoryMovementUC;
        this.createMovementByBarcodeUC = createMovementByBarcodeUC;
        this.list = async (req, res) => {
            try {
                const filters = expenses_dto_1.ExpenseQueryDto.parse(req.query);
                let tzOffsetMinutes = filters.tzOffsetMinutes ?? 0;
                if (tzOffsetMinutes < -720 || tzOffsetMinutes > 840) {
                    throw new Error("tzOffsetMinutes inválido");
                }
                const data = await this.listUC.exec({
                    ...filters,
                    from: this.parseDate(filters.from, false, tzOffsetMinutes),
                    to: this.parseDate(filters.to, true, tzOffsetMinutes),
                });
                return (0, apiResponse_1.success)(res, data);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error listing expenses", 400, err);
            }
        };
        this.get = async (req, res) => {
            try {
                const id = Number(req.params.id);
                if (!Number.isFinite(id) || id <= 0)
                    return (0, apiResponse_1.fail)(res, "Id inválido", 400);
                const expense = await this.getUC.exec(id);
                if (!expense)
                    return (0, apiResponse_1.fail)(res, "Not found", 404);
                return (0, apiResponse_1.success)(res, expense);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error fetching expense", 400, err);
            }
        };
        this.create = async (req, res) => {
            try {
                const dto = expenses_dto_1.CreateExpenseDto.parse(req.body);
                const recordedByUserId = req.auth?.userId;
                if (!recordedByUserId)
                    return (0, apiResponse_1.fail)(res, "Unauthorized", 401);
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
                return (0, apiResponse_1.success)(res, expense, "Created", 201);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error creating expense", 400, err);
            }
        };
        this.update = async (req, res) => {
            try {
                const id = Number(req.params.id);
                if (!Number.isFinite(id) || id <= 0)
                    return (0, apiResponse_1.fail)(res, "Id inválido", 400);
                const dto = expenses_dto_1.UpdateExpenseDto.parse(req.body);
                const expense = await this.updateUC.exec(id, {
                    ...dto,
                    incurredAt: dto.incurredAt ? new Date(dto.incurredAt) : undefined,
                });
                return (0, apiResponse_1.success)(res, expense, "Updated");
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error updating expense", 400, err);
            }
        };
        this.remove = async (req, res) => {
            try {
                const id = Number(req.params.id);
                if (!Number.isFinite(id) || id <= 0)
                    return (0, apiResponse_1.fail)(res, "Id inválido", 400);
                await this.deleteUC.exec(id);
                return (0, apiResponse_1.success)(res, null, "Deleted", 204);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error deleting expense", 400, err);
            }
        };
        this.summary = async (req, res) => {
            try {
                const filters = expenses_dto_1.ExpensesSummaryQueryDto.parse(req.query);
                let tzOffsetMinutes = filters.tzOffsetMinutes ?? 0;
                if (tzOffsetMinutes < -720 || tzOffsetMinutes > 840) {
                    throw new Error("tzOffsetMinutes inválido");
                }
                const data = await this.summaryUC.exec({
                    ...filters,
                    from: this.parseDate(filters.from, false, tzOffsetMinutes),
                    to: this.parseDate(filters.to, true, tzOffsetMinutes),
                });
                return (0, apiResponse_1.success)(res, data);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error getting expenses summary", 400, err);
            }
        };
    }
    parseDate(value, endOfDay = false, tzOffsetMinutes = 0) {
        if (!value)
            return undefined;
        const dateOnlyRegex = /^\d{4}-\d{2}-\d{2}$/;
        const localDateTimeRegex = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d{1,3})?)?$/;
        const normalizeLocal = (datePart, timePart) => {
            const [yearStr, monthStr, dayStr] = datePart.split("-");
            const year = Number(yearStr);
            const month = Number(monthStr);
            const day = Number(dayStr);
            if (!year || !month || !day)
                throw new Error(`Fecha inválida: ${value}`);
            let hours = 0;
            let minutes = 0;
            let seconds = 0;
            let millis = 0;
            if (timePart) {
                const match = timePart.match(/^(\d{2}):(\d{2})(?::(\d{2})(?:\.(\d{1,3}))?)?$/);
                if (!match)
                    throw new Error(`Hora inválida: ${value}`);
                hours = Number(match[1]);
                minutes = Number(match[2]);
                seconds = match[3] ? Number(match[3]) : 0;
                millis = match[4] ? Number(match[4].padEnd(3, "0")) : 0;
            }
            else if (endOfDay) {
                hours = 23;
                minutes = 59;
                seconds = 59;
                millis = 999;
            }
            const utcMillis = Date.UTC(year, month - 1, day, hours, minutes, seconds, millis);
            return new Date(utcMillis - tzOffsetMinutes * 60000);
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
    async createInventoryRecord(payload, userId) {
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
exports.ExpensesController = ExpensesController;
//# sourceMappingURL=ExpensesController.js.map