import type { Response } from "express";
import { success, fail } from "../utils/apiResponse";
import {
  CreateOrderDto,
  AddOrderItemDto,
  UpdateOrderItemStatusDto,
  AddPaymentDto,
  UpdateOrderItemDto,
  SplitOrderByItemsDto,
  UpdateOrderMetaDto,
  UpdateOrderStatusDto,
  ListOrdersQueryDto,
  OrderItemStatusEnum,
  ServiceTypeEnum,
  OrderSourceEnum,
} from "../dtos/orders.dto";
import { RefundOrderDto } from "../dtos/refund.dto";
import { CreateOrder } from "../../core/usecases/orders/CreateOrder";
import { AddOrderItem } from "../../core/usecases/orders/AddOrderItem";
import { UpdateOrderItemStatus } from "../../core/usecases/orders/UpdateOrderItemStatus";
import { AddPayment } from "../../core/usecases/orders/AddPayment";
import { GetOrderDetail } from "../../core/usecases/orders/GetOrderDetail";
import { ListKDS } from "../../core/usecases/orders/ListKDS";
import { UpdateOrderItem } from "../../core/usecases/orders/UpdateOrderItem";
import { RemoveOrderItem } from "../../core/usecases/orders/RemoveOrderItem";
import { SplitOrderByItems } from "../../core/usecases/orders/SplitOrderByItems";
import { UpdateOrderMeta } from "../../core/usecases/orders/UpdateOrderMeta";
import { UpdateOrderStatus } from "../../core/usecases/orders/UpdateOrderStatus";
import { ListOrders } from "../../core/usecases/orders/ListOrders";
import { RefundOrder } from "../../core/usecases/orders/RefundOrder";
import type { AuthRequest } from "../middlewares/authenticate";
import { AdminAuthService } from "../../infra/services/AdminAuthService";

function ensureNumericId(param: string | undefined) {
  const id = Number(param);
  if (!param || Number.isNaN(id) || id <= 0) {
    throw new Error("Id inválido");
  }
  return id;
}

const dateOnlyRegex = /^\d{4}-\d{2}-\d{2}$/;
const localDateTimeRegex = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d{1,3})?)?$/;

function parseDateParam(value?: string, endOfDay = false, tzOffsetMinutes = 0) {
  if (!value) return undefined;

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

export class OrdersController {
  constructor(
    private createOrderUC: CreateOrder,
    private addItemUC: AddOrderItem,
    private updateItemStatusUC: UpdateOrderItemStatus,
    private addPaymentUC: AddPayment,
    private getOrderDetailUC: GetOrderDetail,
    private listKDSUC: ListKDS,
    private updateOrderItemUC: UpdateOrderItem,
    private removeOrderItemUC: RemoveOrderItem,
    private splitOrderByItemsUC: SplitOrderByItems,
    private updateOrderMetaUC: UpdateOrderMeta,
    private updateOrderStatusUC: UpdateOrderStatus,
    private listOrdersUC: ListOrders,
    private refundOrderUC: RefundOrder,
    private adminAuthService: AdminAuthService
  ) {}

  list = async (req: AuthRequest, res: Response) => {
    try {
      const filtersDto = ListOrdersQueryDto.parse(req.query);
      const rawTz = req.query.tzOffsetMinutes;
      let tzOffsetMinutes = 0;
      if (rawTz !== undefined) {
        tzOffsetMinutes = Number(rawTz);
        if (
          Number.isNaN(tzOffsetMinutes) ||
          tzOffsetMinutes < -720 ||
          tzOffsetMinutes > 840
        ) {
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
      return success(res, orders);
    } catch (e: any) {
      return fail(res, e?.message || "Error listando pedidos", 400, e);
    }
  };

  create = async (req: AuthRequest, res: Response) => {
    try {
      const dto = CreateOrderDto.parse(req.body);
      const userId = req.auth?.userId;
      if (!userId) return fail(res, "Unauthorized", 401);
      const { items, ...orderInput } = dto;
      const order = await this.createOrderUC.exec(orderInput, userId);

      if (items?.length) {
        for (const item of items) {
          await this.addItemUC.exec(order.id, item);
        }
      }

      const details = await this.getOrderDetailUC.exec(order.id);
      return success(res, details ?? order, "Created", 201);
    } catch (e: any) {
      return fail(res, e?.message || "Error creating order", 400, e);
    }
  };

  get = async (req: AuthRequest, res: Response) => {
    try {
      const id = ensureNumericId(req.params.id);
      const data = await this.getOrderDetailUC.exec(id);
      if (!data) return fail(res, "Not found", 404);
      return success(res, data);
    } catch (e: any) {
      const status = e?.message === "Id inválido" ? 400 : 500;
      return fail(res, e?.message || "Error fetching order", status, e);
    }
  };

  addItem = async (req: AuthRequest, res: Response) => {
    try {
      const id = ensureNumericId(req.params.id);
      const dto = AddOrderItemDto.parse(req.body);
      const result = await this.addItemUC.exec(id, dto);
      return success(res, result, "Item added", 201);
    } catch (e: any) {
      const status = e?.message === "Id inválido" ? 400 : 500;
      return fail(res, e?.message || "Error adding item", status, e);
    }
  };

  setItemStatus = async (req: AuthRequest, res: Response) => {
    try {
      const itemId = ensureNumericId(req.params.itemId);
      const dto = UpdateOrderItemStatusDto.parse(req.body);
      await this.updateItemStatusUC.exec(itemId, dto.status);
      return success(res, null, "Updated");
    } catch (e: any) {
      const status = e?.message === "Id inválido" ? 400 : 500;
      return fail(res, e?.message || "Error updating status", status, e);
    }
  };

  addPayment = async (req: AuthRequest, res: Response) => {
    try {
      const id = ensureNumericId(req.params.id);
      const dto = AddPaymentDto.parse(req.body);
      const receivedByUserId = req.auth?.userId;
      if (!receivedByUserId) return fail(res, "Unauthorized", 401);
      const payment = await this.addPaymentUC.exec(id, { ...dto, receivedByUserId });
      return success(res, payment, "Paid", 201);
    } catch (e: any) {
      const status = e?.message === "Id inválido"
        ? 400
        : e?.message?.toLowerCase?.().includes("ya pagado")
        ? 409
        : 500;
      return fail(res, e?.message || "Error adding payment", status, e);
    }
  };

  kds = async (req: AuthRequest, res: Response) => {
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
        .map((s) => OrderItemStatusEnum.parse(s))
        .filter((s): s is "NEW" | "IN_PROGRESS" | "READY" => allowed.has(s));

      const serviceType = req.query.serviceType
        ? ServiceTypeEnum.parse(String(req.query.serviceType))
        : undefined;
      const source = req.query.source
        ? OrderSourceEnum.parse(String(req.query.source))
        : undefined;
      const tableId =
        req.query.tableId !== undefined && req.query.tableId !== null
          ? Number(req.query.tableId)
          : undefined;
      if (tableId !== undefined && (Number.isNaN(tableId) || tableId <= 0)) {
        throw new Error("tableId inválido");
      }

      const rawTz = req.query.tzOffsetMinutes;
      let tzOffsetMinutes = 0;
      if (rawTz !== undefined) {
        tzOffsetMinutes = Number(rawTz);
        if (
          Number.isNaN(tzOffsetMinutes) ||
          tzOffsetMinutes < -720 ||
          tzOffsetMinutes > 840
        ) {
          throw new Error("tzOffsetMinutes inválido");
        }
      }

      const filters = {
        statuses: statuses.length ? statuses : undefined,
        serviceType,
        source,
        tableId,
        from: parseDateParam(req.query.from as string | undefined, false, tzOffsetMinutes),
        to: parseDateParam(req.query.to as string | undefined, true, tzOffsetMinutes),
      };

      const rows = await this.listKDSUC.exec(filters);
      return success(res, rows);
    } catch (e: any) {
      return fail(res, e?.message || "Error KDS", 400, e);
    }
  };

  updateItem = async (req: AuthRequest, res: Response) => {
    try {
      const itemId = ensureNumericId(req.params.itemId);
      const dto = UpdateOrderItemDto.parse(req.body);
      await this.updateOrderItemUC.exec(itemId, {
        quantity: dto.quantity,
        notes: dto.notes,
        replaceModifiers: dto.replaceModifiers,
      });
      return success(res, null, "Updated");
    } catch (e: any) {
      const status = e?.message === "Id inválido" ? 400 : 500;
      return fail(res, e?.message || "Error updating order item", status, e);
    }
  };

  removeItem = async (req: AuthRequest, res: Response) => {
    try {
      const itemId = ensureNumericId(req.params.itemId);
      await this.removeOrderItemUC.exec(itemId);
      return success(res, null, "Deleted");
    } catch (e: any) {
      const status = e?.message === "Id inválido" ? 400 : 500;
      return fail(res, e?.message || "Error deleting order item", status, e);
    }
  };

  splitByItems = async (req: AuthRequest, res: Response) => {
    try {
      const orderId = ensureNumericId(req.params.id);
      const dto = SplitOrderByItemsDto.parse(req.body);
      const servedByUserId = req.auth?.userId;
      if (!servedByUserId) return fail(res, "Unauthorized", 401);
      const result = await this.splitOrderByItemsUC.exec(orderId, {
        ...dto,
        servedByUserId,
      });
      return success(res, result, "Order split", 201);
    } catch (e: any) {
      const status = e?.message === "Id inválido" ? 400 : 500;
      return fail(res, e?.message || "Error splitting order", status, e);
    }
  };

  updateMeta = async (req: AuthRequest, res: Response) => {
    try {
      const orderId = ensureNumericId(req.params.id);
      const dto = UpdateOrderMetaDto.parse(req.body);
      await this.updateOrderMetaUC.exec(orderId, dto);
      return success(res, null, "Updated");
    } catch (e: any) {
      const status = e?.message === "Id inválido" ? 400 : 500;
      return fail(res, e?.message || "Error updating order meta", status, e);
    }
  };

  updateStatus = async (req: AuthRequest, res: Response) => {
    try {
      const orderId = ensureNumericId(req.params.id);
      const dto = UpdateOrderStatusDto.parse(req.body);
      await this.updateOrderStatusUC.exec(orderId, dto.status);
      return success(res, null, "Updated");
    } catch (e: any) {
      const status =
        e?.message === "Id inválido"
          ? 400
          : e?.message?.toLowerCase?.().includes("transición") ||
            e?.message?.toLowerCase?.().includes("cerrar")
          ? 409
          : 500;
      return fail(res, e?.message || "Error updating order status", status, e);
    }
  };

  refund = async (req: AuthRequest, res: Response) => {
    try {
      const orderId = ensureNumericId(req.params.id);
      const dto = RefundOrderDto.parse(req.body);
      const admin = await this.adminAuthService.verify({
        adminEmail: dto.adminEmail,
        adminPin: dto.adminPin,
        password: dto.password,
      });
      const result = await this.refundOrderUC.exec(orderId, {
        adminUserId: admin.id,
        reason: dto.reason ?? null,
      });
      return success(res, result, "Refunded");
    } catch (e: any) {
      const status = e?.status || (e?.message?.toLowerCase?.().includes("pedido") ? 400 : 500);
      return fail(res, e?.message || "Error refunding order", status, e);
    }
  };
}
