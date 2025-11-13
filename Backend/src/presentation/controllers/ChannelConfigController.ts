import type { Response } from "express";
import type { AuthRequest } from "../middlewares/authenticate";
import { success, fail } from "../utils/apiResponse";
import {
  UpsertChannelConfigBodyDto,
  UpsertChannelConfigParamsDto,
} from "../dtos/channelConfig.dto";
import { ListChannelConfigs } from "../../core/usecases/channelConfig/ListChannelConfigs";
import { SetChannelConfig } from "../../core/usecases/channelConfig/SetChannelConfig";

export class ChannelConfigController {
  constructor(
    private readonly listUC: ListChannelConfigs,
    private readonly setUC: SetChannelConfig
  ) {}

  list = async (_req: AuthRequest, res: Response) => {
    try {
      const rows = await this.listUC.exec();
      return success(
        res,
        rows.map((row) => ({
          source: row.source,
          markupPct: Number(row.markupPct),
          updatedAt: row.updatedAt,
        }))
      );
    } catch (e: any) {
      return fail(res, e?.message || "Error listing channel config", 400, e);
    }
  };

  set = async (req: AuthRequest, res: Response) => {
    try {
      const params = UpsertChannelConfigParamsDto.parse(req.params);
      const body = UpsertChannelConfigBodyDto.parse(req.body);
      const row = await this.setUC.exec(params.source, body.markupPct);
      return success(res, {
        source: row.source,
        markupPct: Number(row.markupPct),
        updatedAt: row.updatedAt,
      });
    } catch (e: any) {
      return fail(res, e?.message || "Error saving channel config", 400, e);
    }
  };
}
