import { z } from 'zod';

export const LoginByEmailDto = z.object({
  email: z.string().email(),
  password: z.string().min(3)
});

export const LoginByPinDto = z.object({
  pinCode: z.string().length(4).regex(/^\d{4}$/),
  password: z.string().min(3)
});
