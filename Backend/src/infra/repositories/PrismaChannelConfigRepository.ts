import { prisma } from "../../lib/prisma";
import type { ChannelConfig } from "@prisma/client";
import type {
  IChannelConfigRepository,
} from "../../core/domain/repositories/IChannelConfigRepository";
import type { OrderSource } from "../../core/domain/repositories/IOrderRepository";

export class PrismaChannelConfigRepository implements IChannelConfigRepository {
  list(): Promise<ChannelConfig[]> {
    return prisma.channelConfig.findMany({ orderBy: { source: "asc" } });
  }

  async upsert(source: OrderSource, markupPct: number): Promise<ChannelConfig> {
    return prisma.channelConfig.upsert({
      where: { source },
      update: { markupPct },
      create: { source, markupPct },
    });
  }
}
