import type { ChannelConfig } from "@prisma/client";
import type { OrderSource } from "./IOrderRepository";
export interface IChannelConfigRepository {
    list(): Promise<ChannelConfig[]>;
    upsert(source: OrderSource, markupPct: number): Promise<ChannelConfig>;
}
//# sourceMappingURL=IChannelConfigRepository.d.ts.map