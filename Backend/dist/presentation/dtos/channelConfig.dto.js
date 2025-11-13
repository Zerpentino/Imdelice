"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpsertChannelConfigBodyDto = exports.UpsertChannelConfigParamsDto = void 0;
const zod_1 = require("zod");
const orders_dto_1 = require("./orders.dto");
exports.UpsertChannelConfigParamsDto = zod_1.z.object({
    source: orders_dto_1.OrderSourceEnum,
});
exports.UpsertChannelConfigBodyDto = zod_1.z.object({
    markupPct: zod_1.z.number().finite().min(0).max(300),
});
//# sourceMappingURL=channelConfig.dto.js.map