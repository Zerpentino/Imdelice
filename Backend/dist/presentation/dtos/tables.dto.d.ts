import { z } from 'zod';
export declare const CreateTableDto: z.ZodObject<{
    name: z.ZodString;
    seats: z.ZodOptional<z.ZodNullable<z.ZodNumber>>;
    isActive: z.ZodOptional<z.ZodBoolean>;
}, z.core.$strip>;
export declare const UpdateTableDto: z.ZodObject<{
    name: z.ZodOptional<z.ZodString>;
    seats: z.ZodOptional<z.ZodNullable<z.ZodNumber>>;
    isActive: z.ZodOptional<z.ZodBoolean>;
}, z.core.$strip>;
//# sourceMappingURL=tables.dto.d.ts.map