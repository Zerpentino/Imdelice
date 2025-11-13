"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.RolesController = void 0;
const apiResponse_1 = require("../utils/apiResponse");
const roles_dto_1 = require("../dtos/roles.dto");
class RolesController {
    constructor(listRoles, getRoleById, createRole, updateRole, deleteRole, setRolePerms // 👈 inyecta por container
    ) {
        this.listRoles = listRoles;
        this.getRoleById = getRoleById;
        this.createRole = createRole;
        this.updateRole = updateRole;
        this.deleteRole = deleteRole;
        this.setRolePerms = setRolePerms;
        this.list = async (_, res) => {
            const roles = await this.listRoles.execute();
            return (0, apiResponse_1.success)(res, roles, "Roles obtenidos");
        };
        this.get = async (req, res) => {
            const id = Number(req.params.id);
            if (Number.isNaN(id))
                return (0, apiResponse_1.fail)(res, "ID inválido", 400);
            const role = await this.getRoleById.execute(id);
            if (!role)
                return (0, apiResponse_1.fail)(res, "Rol no encontrado", 404);
            // ✅ Mapear el join RolePermission → Permission
            const permissions = Array.isArray(role.permissions)
                ? role.permissions.map((rp) => ({
                    id: rp.permission.id,
                    code: rp.permission.code
                }))
                : [];
            return (0, apiResponse_1.success)(res, {
                id: role.id,
                name: role.name,
                description: role.description,
                permissions
            }, "Rol encontrado");
        };
        this.create = async (req, res) => {
            const parsed = roles_dto_1.CreateRoleDto.safeParse(req.body);
            if (!parsed.success)
                return (0, apiResponse_1.fail)(res, "Datos inválidos", 400, parsed.error.format());
            const role = await this.createRole.execute({ name: parsed.data.name, description: parsed.data.description ?? null });
            if (parsed.data.permissionCodes?.length) {
                await this.setRolePerms.execute(role.id, parsed.data.permissionCodes);
            }
            const out = await this.getRoleById.execute(role.id);
            return (0, apiResponse_1.success)(res, out, "Rol creado");
        };
        this.update = async (req, res) => {
            const id = Number(req.params.id);
            const parsed = roles_dto_1.UpdateRoleDto.safeParse(req.body);
            if (!parsed.success)
                return (0, apiResponse_1.fail)(res, "Datos inválidos", 400, parsed.error.format());
            const { permissionCodes, ...roleFields } = parsed.data;
            await this.updateRole.execute({ id, ...roleFields });
            if (permissionCodes) {
                await this.setRolePerms.execute(id, permissionCodes);
            }
            // 👇 out puede ser null → checarlo
            const out = await this.getRoleById.execute(id);
            if (!out)
                return (0, apiResponse_1.fail)(res, "Rol no encontrado", 404);
            // 👇 out.permissions existe por el include y el tipo RoleWithPermissions
            const permissions = out.permissions.map((rp) => ({
                id: rp.permission.id,
                code: rp.permission.code
            }));
            return (0, apiResponse_1.success)(res, {
                id: out.id,
                name: out.name,
                description: out.description,
                permissions,
            }, "Rol actualizado");
        };
        this.delete = async (req, res) => {
            const id = Number(req.params.id);
            if (Number.isNaN(id))
                return (0, apiResponse_1.fail)(res, "ID inválido", 400);
            await this.deleteRole.execute(id);
            return (0, apiResponse_1.success)(res, { id }, "Rol eliminado");
        };
    }
}
exports.RolesController = RolesController;
//# sourceMappingURL=RolesController.js.map