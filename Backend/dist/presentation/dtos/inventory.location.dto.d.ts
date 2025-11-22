import { z } from "zod";
export declare const CreateInventoryLocationDto: z.ZodObject<{
    name: z.ZodString;
    code: z.ZodOptional<z.ZodString>;
    type: z.ZodOptional<z.ZodEnum<{
        GENERAL: "GENERAL";
        KITCHEN: "KITCHEN";
        BAR: "BAR";
        STORAGE: "STORAGE";
    }>>;
    isDefault: z.ZodOptional<z.ZodBoolean>;
}, z.core.$strip>;
export declare const UpdateInventoryLocationDto: z.ZodObject<{
    name: z.ZodOptional<z.ZodString>;
    code: z.ZodOptional<z.ZodNullable<z.ZodString>>;
    type: z.ZodOptional<z.ZodEnum<{
        GENERAL: "GENERAL";
        KITCHEN: "KITCHEN";
        BAR: "BAR";
        STORAGE: "STORAGE";
    }>>;
    isDefault: z.ZodOptional<z.ZodBoolean>;
    isActive: z.ZodOptional<z.ZodBoolean>;
}, z.core.$strip>;
//# sourceMappingURL=inventory.location.dto.d.ts.map