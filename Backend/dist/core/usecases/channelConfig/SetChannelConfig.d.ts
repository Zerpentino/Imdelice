import type { IChannelConfigRepository } from "../../domain/repositories/IChannelConfigRepository";
import type { OrderSource } from "../../domain/repositories/IOrderRepository";
import type { ChannelConfig } from "@prisma/client";
export declare class SetChannelConfig {
    private readonly repo;
    constructor(repo: IChannelConfigRepository);
    exec(source: OrderSource, markupPct: number): Promise<ChannelConfig>;
}
//# sourceMappingURL=SetChannelConfig.d.ts.map