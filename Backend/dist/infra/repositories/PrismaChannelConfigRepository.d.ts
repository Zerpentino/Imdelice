import type { ChannelConfig } from "@prisma/client";
import type { IChannelConfigRepository } from "../../core/domain/repositories/IChannelConfigRepository";
import type { OrderSource } from "../../core/domain/repositories/IOrderRepository";
export declare class PrismaChannelConfigRepository implements IChannelConfigRepository {
    list(): Promise<ChannelConfig[]>;
    upsert(source: OrderSource, markupPct: number): Promise<ChannelConfig>;
}
//# sourceMappingURL=PrismaChannelConfigRepository.d.ts.map