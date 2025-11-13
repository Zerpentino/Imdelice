import { Request, Response } from "express";
import { success, fail } from "../utils/apiResponse";
import { CreateRole } from "../../core/usecases/roles/CreateRole";
import { UpdateRole } from "../../core/usecases/roles/UpdateRole";
import { DeleteRole } from "../../core/usecases/roles/DeleteRole";
import { GetRoleById } from "../../core/usecases/roles/GetRoleById";
import { ListRoles } from "../../core/usecases/roles/ListRoles";
import { CreateRoleDto, UpdateRoleDto } from "../dtos/roles.dto";
import { SetRolePermissions } from '../../core/usecases/roles/SetRolePermissions'

export class RolesController {
  constructor(
    private listRoles: ListRoles,
    private getRoleById: GetRoleById,
    private createRole: CreateRole,
    private updateRole: UpdateRole,
    private deleteRole: DeleteRole,
        private setRolePerms: SetRolePermissions // 👈 inyecta por container

  ) {}

  list = async (_: Request, res: Response) => {
    const roles = await this.listRoles.execute();
    return success(res, roles, "Roles obtenidos");
  };

get = async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  if (Number.isNaN(id)) return fail(res, "ID inválido", 400);

  const role: any = await this.getRoleById.execute(id);
  if (!role) return fail(res, "Rol no encontrado", 404);

  // ✅ Mapear el join RolePermission → Permission
  const permissions = Array.isArray(role.permissions)
    ? role.permissions.map((rp: any) => ({
        id: rp.permission.id,
        code: rp.permission.code
      }))
    : [];

  return success(res, {
    id: role.id,
    name: role.name,
    description: role.description,
    permissions
  }, "Rol encontrado");
};


  create = async (req: Request, res: Response) => {
    const parsed = CreateRoleDto.safeParse(req.body)
    if (!parsed.success) return fail(res, "Datos inválidos", 400, parsed.error.format())
    const role = await this.createRole.execute({ name: parsed.data.name, description: parsed.data.description ?? null })
    if (parsed.data.permissionCodes?.length) {
      await this.setRolePerms.execute(role.id, parsed.data.permissionCodes)
    }
    const out = await this.getRoleById.execute(role.id)
    return success(res, out, "Rol creado")
  }

update = async (req: Request, res: Response) => {
  const id = Number(req.params.id)
  const parsed = UpdateRoleDto.safeParse(req.body)
  if (!parsed.success) return fail(res, "Datos inválidos", 400, parsed.error.format())

  const { permissionCodes, ...roleFields } = parsed.data

  await this.updateRole.execute({ id, ...roleFields })
  if (permissionCodes) {
    await this.setRolePerms.execute(id, permissionCodes)
  }

  // 👇 out puede ser null → checarlo
  const out = await this.getRoleById.execute(id)
  if (!out) return fail(res, "Rol no encontrado", 404)

  // 👇 out.permissions existe por el include y el tipo RoleWithPermissions
  const permissions = out.permissions.map((rp: { permission: { id: number; code: string } }) => ({
    id: rp.permission.id,
    code: rp.permission.code
  }))

  return success(res, {
    id: out.id,
    name: out.name,
    description: out.description,
    permissions,
  }, "Rol actualizado")
}




  delete = async (req: Request, res: Response) => {
    const id = Number(req.params.id);
    if (Number.isNaN(id)) return fail(res, "ID inválido", 400);

    await this.deleteRole.execute(id);
    return success(res, { id }, "Rol eliminado");
  };
}
