import { z } from "zod";
export declare const UpsertChannelConfigParamsDto: z.ZodObject<{
    source: z.ZodEnum<{
        POS: "POS";
        UBER: "UBER";
        DIDI: "DIDI";
        RAPPI: "RAPPI";
    }>;
}, z.core.$strip>;
export declare const UpsertChannelConfigBodyDto: z.ZodObject<{
    markupPct: z.ZodNumber;
}, z.core.$strip>;
export type UpsertChannelConfigBodyDtoType = z.infer<typeof UpsertChannelConfigBodyDto>;
//# sourceMappingURL=channelConfig.dto.d.ts.map