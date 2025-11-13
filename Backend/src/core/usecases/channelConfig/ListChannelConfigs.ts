import type { IChannelConfigRepository } from "../../domain/repositories/IChannelConfigRepository";
import type { ChannelConfig } from "@prisma/client";

export class ListChannelConfigs {
  constructor(private readonly repo: IChannelConfigRepository) {}

  exec(): Promise<ChannelConfig[]> {
    return this.repo.list();
  }
}
