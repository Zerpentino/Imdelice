import { z } from "zod";
import { OrderSourceEnum } from "./orders.dto";

export const UpsertChannelConfigParamsDto = z.object({
  source: OrderSourceEnum,
});

export const UpsertChannelConfigBodyDto = z.object({
  markupPct: z.number().finite().min(0).max(300),
});

export type UpsertChannelConfigBodyDtoType = z.infer<typeof UpsertChannelConfigBodyDto>;
