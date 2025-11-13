import { z } from "zod";

export const CreateRoleDto = z.object({
  name: z.string().min(2),
  description: z.string().nullish(),
  permissionCodes: z.array(z.string()).default([]) // 👈
})

export const UpdateRoleDto = z.object({
  name: z.string().min(2).optional(),
  description: z.string().optional(),
  permissionCodes: z.array(z.string()).optional() // 👈
})

export type CreateRoleDtoType = z.infer<typeof CreateRoleDto>;
export type UpdateRoleDtoType = z.infer<typeof UpdateRoleDto>;
