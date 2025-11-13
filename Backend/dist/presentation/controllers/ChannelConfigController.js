"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ChannelConfigController = void 0;
const apiResponse_1 = require("../utils/apiResponse");
const channelConfig_dto_1 = require("../dtos/channelConfig.dto");
class ChannelConfigController {
    constructor(listUC, setUC) {
        this.listUC = listUC;
        this.setUC = setUC;
        this.list = async (_req, res) => {
            try {
                const rows = await this.listUC.exec();
                return (0, apiResponse_1.success)(res, rows.map((row) => ({
                    source: row.source,
                    markupPct: Number(row.markupPct),
                    updatedAt: row.updatedAt,
                })));
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || "Error listing channel config", 400, e);
            }
        };
        this.set = async (req, res) => {
            try {
                const params = channelConfig_dto_1.UpsertChannelConfigParamsDto.parse(req.params);
                const body = channelConfig_dto_1.UpsertChannelConfigBodyDto.parse(req.body);
                const row = await this.setUC.exec(params.source, body.markupPct);
                return (0, apiResponse_1.success)(res, {
                    source: row.source,
                    markupPct: Number(row.markupPct),
                    updatedAt: row.updatedAt,
                });
            }
            catch (e) {
                return (0, apiResponse_1.fail)(res, e?.message || "Error saving channel config", 400, e);
            }
        };
    }
}
exports.ChannelConfigController = ChannelConfigController;
//# sourceMappingURL=ChannelConfigController.js.map