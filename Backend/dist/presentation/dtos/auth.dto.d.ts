import { z } from 'zod';
export declare const LoginByEmailDto: z.ZodObject<{
    email: z.ZodString;
    password: z.ZodString;
}, z.core.$strip>;
export declare const LoginByPinDto: z.ZodObject<{
    pinCode: z.ZodString;
    password: z.ZodString;
}, z.core.$strip>;
//# sourceMappingURL=auth.dto.d.ts.map