import type { Response } from "express";
import type { AuthRequest } from "../middlewares/authenticate";
import { ListChannelConfigs } from "../../core/usecases/channelConfig/ListChannelConfigs";
import { SetChannelConfig } from "../../core/usecases/channelConfig/SetChannelConfig";
export declare class ChannelConfigController {
    private readonly listUC;
    private readonly setUC;
    constructor(listUC: ListChannelConfigs, setUC: SetChannelConfig);
    list: (_req: AuthRequest, res: Response) => Promise<Response<any, Record<string, any>>>;
    set: (req: AuthRequest, res: Response) => Promise<Response<any, Record<string, any>>>;
}
//# sourceMappingURL=ChannelConfigController.d.ts.map