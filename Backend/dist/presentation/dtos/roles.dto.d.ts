import { z } from "zod";
export declare const CreateRoleDto: z.ZodObject<{
    name: z.ZodString;
    description: z.ZodOptional<z.ZodNullable<z.ZodString>>;
    permissionCodes: z.ZodDefault<z.ZodArray<z.ZodString>>;
}, z.core.$strip>;
export declare const UpdateRoleDto: z.ZodObject<{
    name: z.ZodOptional<z.ZodString>;
    description: z.ZodOptional<z.ZodString>;
    permissionCodes: z.ZodOptional<z.ZodArray<z.ZodString>>;
}, z.core.$strip>;
export type CreateRoleDtoType = z.infer<typeof CreateRoleDto>;
export type UpdateRoleDtoType = z.infer<typeof UpdateRoleDto>;
//# sourceMappingURL=roles.dto.d.ts.map