import type { IChannelConfigRepository } from "../../domain/repositories/IChannelConfigRepository";
import type { OrderSource } from "../../domain/repositories/IOrderRepository";
import type { ChannelConfig } from "@prisma/client";

export class SetChannelConfig {
  constructor(private readonly repo: IChannelConfigRepository) {}

  exec(source: OrderSource, markupPct: number): Promise<ChannelConfig> {
    return this.repo.upsert(source, markupPct);
  }
}
