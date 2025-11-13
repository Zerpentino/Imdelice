import { Role } from "../entities/Role";

export interface CreateRoleInput {
  name: string;
  description?: string | null;
}

export interface UpdateRoleInput {
  id: number;
  name?: string;
  description?: string | null;
}
export type RoleWithPermissions = Role & {
  permissions: {                 // RolePermission[]
    permission: { id: number; code: string }
  }[]
}
export interface IRoleRepository {
  list(): Promise<Role[]>;
  // 👇 Cambiamos el retorno para que coincida con tu findUnique include:{ permissions:{ include:{permission:true}} }
  getById(id: number): Promise<RoleWithPermissions | null>;
  getByName(name: string): Promise<Role | null>;
  create(input: CreateRoleInput): Promise<Role>;
  update(input: UpdateRoleInput): Promise<Role>;
  delete(id: number): Promise<void>;
}

