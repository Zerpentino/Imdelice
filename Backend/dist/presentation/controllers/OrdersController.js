"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.OrdersController = void 0;
const apiResponse_1 = require("../utils/apiResponse");
const orders_dto_1 = require("../dtos/orders.dto");
const refund_dto_1 = require("../dtos/refund.dto");
function ensureNumericId(param) {
    const id = Number(param);
    if (!param || Number.isNaN(id) || id <= 0) {
        throw new Error("Id inválido");
    }
    return id;
}
const dateOnlyRegex = /^\d{4}-\d{2}-\d{2}$/;
const localDateTimeRegex = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d{1,3})?)?$/;
function parseDateParam(value, endOfDay = false, tzOffsetMinutes = 0) {
    if (!value)
        return undefined;
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
class OrdersController {
    constructor(createOrderUC, addItemUC, updateItemStatusUC, addPaymentUC, getOrderDetailUC, listKDSUC, updateOrderItemUC, removeOrderItemUC, splitOrderByItemsUC, updateOrderMetaUC, updateOrderStatusUC, listOrdersUC, refundOrderUC, adminAuthService) {
        this.createOrderUC = createOrderUC;
        this.addItemUC = addItemUC;
        this.updateItemStatusUC = updateItemStatusUC;
        this.addPaymentUC = addPaymentUC;
        this.getOrderDetailUC = getOrderDetailUC;
        this.listKDSUC = listKDSUC;
        this.updateOrderItemUC = updateOrderItemUC;
        this.removeOrderItemUC = removeOrderItemUC;
        this.splitOrderByItemsUC = splitOrderByItemsUC;
        this.updateOrderMetaUC = updateOrderMetaUC;
        this.updateOrderStatusUC = updateOrderStatusUC;
        this.listOrdersUC = listOrdersUC;
        this.refundOrderUC = refundOrderUC;
        this.adminAuthService = adminAuthService;
        this.list = async (req, res) => {
            try {
                const filtersDto = orders_dto_1.ListOrdersQueryDto.parse(req.query);
                const rawTz = req.query.tzOffsetMinutes;
                let tzOffsetMinutes = 0;
                if (rawTz !== undefined) {
                    tzOffsetMinutes = Number(rawTz);
                    if (Number.isNaN(tzOffsetMinutes) ||
                        tzOffsetMinutes < -720 ||
                        tzOffsetMinutes > 840) {
                        throw new Error("tzOffsetMinutes inválido");
                    }
                }
                const filters = {
                    statuses: filtersDto.statuses,
                    serviceType: filtersDto.serviceType,
                    source: filtersDto.source,
                    tableId: filtersDto.tableId,
                    search: filtersDto.search?.trim() || undefined,
                    from: parseDateParam(filtersDto.from, false, tzOffsetMinutes),
                    to: parseDateParam(filtersDto.to, true, tzOffsetMinutes),
                };
                const orders = await this.listOrdersUC.exec(filters);
                return (0, apiResponse_1.success)(res, orders);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || "Error listando pedidos", 400, e);
            }
        };
        this.create = async (req, res) => {
            try {
                const dto = orders_dto_1.CreateOrderDto.parse(req.body);
                const userId = req.auth?.userId;
                if (!userId)
                    return (0, apiResponse_1.fail)(res, "Unauthorized", 401);
                const { items, ...orderInput } = dto;
                const order = await this.createOrderUC.exec(orderInput, userId);
                if (items?.length) {
                    for (const item of items) {
                        await this.addItemUC.exec(order.id, item);
                    }
                }
                const details = await this.getOrderDetailUC.exec(order.id);
                return (0, apiResponse_1.success)(res, details ?? order, "Created", 201);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || "Error creating order", 400, e);
            }
        };
        this.get = async (req, res) => {
            try {
                const id = ensureNumericId(req.params.id);
                const data = await this.getOrderDetailUC.exec(id);
                if (!data)
                    return (0, apiResponse_1.fail)(res, "Not found", 404);
                return (0, apiResponse_1.success)(res, data);
            }
            catch (e) {
                const status = e?.message === "Id inválido" ? 400 : 500;
                return (0, apiResponse_1.fail)(res, e?.message || "Error fetching order", status, e);
            }
        };
        this.addItem = async (req, res) => {
            try {
                const id = ensureNumericId(req.params.id);
                const dto = orders_dto_1.AddOrderItemDto.parse(req.body);
                const result = await this.addItemUC.exec(id, dto);
                return (0, apiResponse_1.success)(res, result, "Item added", 201);
            }
            catch (e) {
                const status = e?.message === "Id inválido" ? 400 : 500;
                return (0, apiResponse_1.fail)(res, e?.message || "Error adding item", status, e);
            }
        };
        this.setItemStatus = async (req, res) => {
            try {
                const itemId = ensureNumericId(req.params.itemId);
                const dto = orders_dto_1.UpdateOrderItemStatusDto.parse(req.body);
                await this.updateItemStatusUC.exec(itemId, dto.status);
                return (0, apiResponse_1.success)(res, null, "Updated");
            }
            catch (e) {
                const status = e?.message === "Id inválido" ? 400 : 500;
                return (0, apiResponse_1.fail)(res, e?.message || "Error updating status", status, e);
            }
        };
        this.addPayment = async (req, res) => {
            try {
                const id = ensureNumericId(req.params.id);
                const dto = orders_dto_1.AddPaymentDto.parse(req.body);
                const receivedByUserId = req.auth?.userId;
                if (!receivedByUserId)
                    return (0, apiResponse_1.fail)(res, "Unauthorized", 401);
                const payment = await this.addPaymentUC.exec(id, { ...dto, receivedByUserId });
                return (0, apiResponse_1.success)(res, payment, "Paid", 201);
            }
            catch (e) {
                const status = e?.message === "Id inválido"
                    ? 400
                    : e?.message?.toLowerCase?.().includes("ya pagado")
                        ? 409
                        : 500;
                return (0, apiResponse_1.fail)(res, e?.message || "Error adding payment", status, e);
            }
        };
        this.kds = async (req, res) => {
            try {
                const rawStatuses = (req.query.statuses ?? req.query.status ?? "NEW,IN_PROGRESS");
                const statusesList = (Array.isArray(rawStatuses) ? rawStatuses.join(",") : String(rawStatuses))
                    .split(",")
                    .map((s) => s.trim())
                    .filter(Boolean);
                const allowed = new Set(["NEW", "IN_PROGRESS", "READY"]);
                const statuses = statusesList
                    .map((s) => s.toUpperCase())
                    .filter((s) => allowed.has(s))
                    .map((s) => orders_dto_1.OrderItemStatusEnum.parse(s))
                    .filter((s) => allowed.has(s));
                const serviceType = req.query.serviceType
                    ? orders_dto_1.ServiceTypeEnum.parse(String(req.query.serviceType))
                    : undefined;
                const source = req.query.source
                    ? orders_dto_1.OrderSourceEnum.parse(String(req.query.source))
                    : undefined;
                const tableId = req.query.tableId !== undefined && req.query.tableId !== null
                    ? Number(req.query.tableId)
                    : undefined;
                if (tableId !== undefined && (Number.isNaN(tableId) || tableId <= 0)) {
                    throw new Error("tableId inválido");
                }
                const rawTz = req.query.tzOffsetMinutes;
                let tzOffsetMinutes = 0;
                if (rawTz !== undefined) {
                    tzOffsetMinutes = Number(rawTz);
                    if (Number.isNaN(tzOffsetMinutes) ||
                        tzOffsetMinutes < -720 ||
                        tzOffsetMinutes > 840) {
                        throw new Error("tzOffsetMinutes inválido");
                    }
                }
                const filters = {
                    statuses: statuses.length ? statuses : undefined,
                    serviceType,
                    source,
                    tableId,
                    from: parseDateParam(req.query.from, false, tzOffsetMinutes),
                    to: parseDateParam(req.query.to, true, tzOffsetMinutes),
                };
                const rows = await this.listKDSUC.exec(filters);
                return (0, apiResponse_1.success)(res, rows);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || "Error KDS", 400, e);
            }
        };
        this.updateItem = async (req, res) => {
            try {
                const itemId = ensureNumericId(req.params.itemId);
                const dto = orders_dto_1.UpdateOrderItemDto.parse(req.body);
                await this.updateOrderItemUC.exec(itemId, {
                    quantity: dto.quantity,
                    notes: dto.notes,
                    replaceModifiers: dto.replaceModifiers,
                });
                return (0, apiResponse_1.success)(res, null, "Updated");
            }
            catch (e) {
                const status = e?.message === "Id inválido" ? 400 : 500;
                return (0, apiResponse_1.fail)(res, e?.message || "Error updating order item", status, e);
            }
        };
        this.removeItem = async (req, res) => {
            try {
                const itemId = ensureNumericId(req.params.itemId);
                await this.removeOrderItemUC.exec(itemId);
                return (0, apiResponse_1.success)(res, null, "Deleted");
            }
            catch (e) {
                const status = e?.message === "Id inválido" ? 400 : 500;
                return (0, apiResponse_1.fail)(res, e?.message || "Error deleting order item", status, e);
            }
        };
        this.splitByItems = async (req, res) => {
            try {
                const orderId = ensureNumericId(req.params.id);
                const dto = orders_dto_1.SplitOrderByItemsDto.parse(req.body);
                const servedByUserId = req.auth?.userId;
                if (!servedByUserId)
                    return (0, apiResponse_1.fail)(res, "Unauthorized", 401);
                const result = await this.splitOrderByItemsUC.exec(orderId, {
                    ...dto,
                    servedByUserId,
                });
                return (0, apiResponse_1.success)(res, result, "Order split", 201);
            }
            catch (e) {
                const status = e?.message === "Id inválido" ? 400 : 500;
                return (0, apiResponse_1.fail)(res, e?.message || "Error splitting order", status, e);
            }
        };
        this.updateMeta = async (req, res) => {
            try {
                const orderId = ensureNumericId(req.params.id);
                const dto = orders_dto_1.UpdateOrderMetaDto.parse(req.body);
                await this.updateOrderMetaUC.exec(orderId, dto);
                return (0, apiResponse_1.success)(res, null, "Updated");
            }
            catch (e) {
                const status = e?.message === "Id inválido" ? 400 : 500;
                return (0, apiResponse_1.fail)(res, e?.message || "Error updating order meta", status, e);
            }
        };
        this.updateStatus = async (req, res) => {
            try {
                const orderId = ensureNumericId(req.params.id);
                const dto = orders_dto_1.UpdateOrderStatusDto.parse(req.body);
                await this.updateOrderStatusUC.exec(orderId, dto.status);
                return (0, apiResponse_1.success)(res, null, "Updated");
            }
            catch (e) {
                const status = e?.message === "Id inválido"
                    ? 400
                    : e?.message?.toLowerCase?.().includes("transición") ||
                        e?.message?.toLowerCase?.().includes("cerrar")
                        ? 409
                        : 500;
                return (0, apiResponse_1.fail)(res, e?.message || "Error updating order status", status, e);
            }
        };
        this.refund = async (req, res) => {
            try {
                const orderId = ensureNumericId(req.params.id);
                const dto = refund_dto_1.RefundOrderDto.parse(req.body);
                const admin = await this.adminAuthService.verify({
                    adminEmail: dto.adminEmail,
                    adminPin: dto.adminPin,
                    password: dto.password,
                });
                const result = await this.refundOrderUC.exec(orderId, {
                    adminUserId: admin.id,
                    reason: dto.reason ?? null,
                });
                return (0, apiResponse_1.success)(res, result, "Refunded");
            }
            catch (e) {
                const status = e?.status || (e?.message?.toLowerCase?.().includes("pedido") ? 400 : 500);
                return (0, apiResponse_1.fail)(res, e?.message || "Error refunding order", status, e);
            }
        };
    }
}
exports.OrdersController = OrdersController;
//# sourceMappingURL=OrdersController.js.map