import { z } from 'zod';
export declare const CreateUserDto: z.ZodObject<{
    email: z.ZodString;
    name: z.ZodOptional<z.ZodString>;
    roleId: z.ZodNumber;
    password: z.ZodString;
    pinCode: z.ZodOptional<z.ZodString>;
}, z.core.$strip>;
export declare const UpdateUserDto: z.ZodObject<{
    email: z.ZodOptional<z.ZodString>;
    name: z.ZodOptional<z.ZodString>;
    roleId: z.ZodOptional<z.ZodNumber>;
    password: z.ZodOptional<z.ZodString>;
    pinCode: z.ZodOptional<z.ZodNullable<z.ZodString>>;
}, z.core.$strip>;
export type CreateUserDtoType = z.infer<typeof CreateUserDto>;
export type UpdateUserDtoType = z.infer<typeof UpdateUserDto>;
//# sourceMappingURL=users.dto.d.ts.map