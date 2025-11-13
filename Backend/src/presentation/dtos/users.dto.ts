import { z } from 'zod';

export const CreateUserDto = z.object({
  email: z.string().email(),
  name: z.string().optional(),
  roleId: z.number().int().positive(),
  password: z.string().min(3),
  pinCode: z.string().length(4).regex(/^\d{4}$/).optional() // '1234'
});

export const UpdateUserDto = z.object({
  email: z.string().email().optional(),
  name: z.string().optional(),
  roleId: z.number().int().positive().optional(),
  password: z.string().min(3).optional(),
  pinCode: z.string().length(4).regex(/^\d{4}$/).nullable().optional()
});

export type CreateUserDtoType = z.infer<typeof CreateUserDto>;
export type UpdateUserDtoType = z.infer<typeof UpdateUserDto>;
