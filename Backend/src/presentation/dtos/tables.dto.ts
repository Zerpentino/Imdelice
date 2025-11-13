import { z } from 'zod';

const seatsSchema = z
  .number()
  .int()
  .positive()
  .nullable()
  .optional();

export const CreateTableDto = z.object({
  name: z.string().min(1),
  seats: seatsSchema,
  isActive: z.boolean().optional()
});

export const UpdateTableDto = z
  .object({
    name: z.string().min(1).optional(),
    seats: seatsSchema,
    isActive: z.boolean().optional()
  })
  .refine((data) => Object.keys(data).length > 0, {
    message: 'Debe proporcionar al menos un campo para actualizar.'
  });
