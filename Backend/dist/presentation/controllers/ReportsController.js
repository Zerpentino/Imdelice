"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ReportsController = void 0;
const apiResponse_1 = require("../utils/apiResponse");
const reports_dto_1 = require("../dtos/reports.dto");
const dateOnlyRegex = /^\d{4}-\d{2}-\d{2}$/;
const localDateTimeRegex = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d{1,3})?)?$/;
function parseDate(value, endOfDay = false, tzOffsetMinutes = 0) {
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
class ReportsController {
    constructor(paymentsReportUC) {
        this.paymentsReportUC = paymentsReportUC;
        this.payments = async (req, res) => {
            try {
                const query = reports_dto_1.PaymentsReportQueryDto.parse(req.query);
                const tzOffsetRaw = req.query.tzOffsetMinutes;
                let tzOffsetMinutes = 0;
                if (tzOffsetRaw !== undefined) {
                    tzOffsetMinutes = Number(tzOffsetRaw);
                    if (Number.isNaN(tzOffsetMinutes) ||
                        tzOffsetMinutes < -720 ||
                        tzOffsetMinutes > 840) {
                        throw new Error("tzOffsetMinutes inválido");
                    }
                }
                const filters = {
                    from: parseDate(query.from, false, tzOffsetMinutes),
                    to: parseDate(query.to, true, tzOffsetMinutes),
                    includeOrders: query.includeOrders,
                };
                const report = await this.paymentsReportUC.exec(filters);
                return (0, apiResponse_1.success)(res, report);
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || "Error generating payments report", 400, e);
            }
        };
    }
}
exports.ReportsController = ReportsController;
//# sourceMappingURL=ReportsController.js.map