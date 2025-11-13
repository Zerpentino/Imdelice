import type { IChannelConfigRepository } from "../../domain/repositories/IChannelConfigRepository";
import type { ChannelConfig } from "@prisma/client";
export declare class ListChannelConfigs {
    private readonly repo;
    constructor(repo: IChannelConfigRepository);
    exec(): Promise<ChannelConfig[]>;
}
//# sourceMappingURL=ListChannelConfigs.d.ts.map