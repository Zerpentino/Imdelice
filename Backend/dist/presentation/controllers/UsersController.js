"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UsersController = void 0;
const users_dto_1 = require("../dtos/users.dto");
const apiResponse_1 = require("../utils/apiResponse");
class UsersController {
    constructor(listUsers, getUserById, createUser, updateUser, deleteUser) {
        this.listUsers = listUsers;
        this.getUserById = getUserById;
        this.createUser = createUser;
        this.updateUser = updateUser;
        this.deleteUser = deleteUser;
        // GET /api/users
        this.list = async (_req, res) => {
            const users = await this.listUsers.execute();
            return (0, apiResponse_1.success)(res, users, "Usuarios obtenidos");
        };
        // GET /api/users/:id
        this.get = async (req, res) => {
            const id = Number(req.params.id);
            if (Number.isNaN(id))
                return (0, apiResponse_1.fail)(res, "ID inválido", 400);
            const user = await this.getUserById.execute(id);
            if (!user)
                return (0, apiResponse_1.fail)(res, "Usuario no encontrado", 404);
            return (0, apiResponse_1.success)(res, user, "Usuario encontrado");
        };
        // POST /api/users
        this.create = async (req, res) => {
            const parsed = users_dto_1.CreateUserDto.safeParse(req.body);
            if (!parsed.success) {
                return (0, apiResponse_1.fail)(res, "Datos inválidos", 400, parsed.error.format());
            }
            // Regla de negocio (ya implementada en usecase):
            // - bloquea dominios (ej: @spam.com)
            // - valida email único vía repo.getByEmail
            const created = await this.createUser.execute(parsed.data);
            return (0, apiResponse_1.success)(res, created, "Usuario creado", 201);
        };
        // PUT /api/users/:id
        this.update = async (req, res) => {
            const id = Number(req.params.id);
            if (Number.isNaN(id))
                return (0, apiResponse_1.fail)(res, "ID inválido", 400);
            console.log("olaaa");
            const parsed = users_dto_1.UpdateUserDto.safeParse(req.body);
            if (!parsed.success) {
                return (0, apiResponse_1.fail)(res, "Datos inválidos", 400, parsed.error.format());
            }
            const updated = await this.updateUser.execute({ id, ...parsed.data });
            return (0, apiResponse_1.success)(res, updated, "Usuario actualizado");
        };
        // DELETE /api/users/:id
        this.delete = async (req, res) => {
            const id = Number(req.params.id);
            if (Number.isNaN(id))
                return (0, apiResponse_1.fail)(res, "ID inválido", 400);
            await this.deleteUser.execute(id);
            return (0, apiResponse_1.success)(res, { id }, "Usuario eliminado");
        };
    }
}
exports.UsersController = UsersController;
//# sourceMappingURL=UsersController.js.map