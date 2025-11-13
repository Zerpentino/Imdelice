// src/presentation/controllers/UsersController.ts
import { Request, Response } from "express";
import { CreateUser } from "../../core/usecases/users/CreateUser";
import { DeleteUser } from "../../core/usecases/users/DeleteUser";
import { GetUserById } from "../../core/usecases/users/GetUserById";
import { ListUsers } from "../../core/usecases/users/ListUsers";
import { UpdateUser } from "../../core/usecases/users/UpdateUser";
import { CreateUserDto, UpdateUserDto } from "../dtos/users.dto";
import { success, fail } from "../utils/apiResponse";

export class UsersController {
  constructor(
    private listUsers: ListUsers,
    private getUserById: GetUserById,
    private createUser: CreateUser,
    private updateUser: UpdateUser,
    private deleteUser: DeleteUser
  ) {}

  // GET /api/users
  list = async (_req: Request, res: Response) => {
    const users = await this.listUsers.execute();
    return success(res, users, "Usuarios obtenidos");
  };

  // GET /api/users/:id
  get = async (req: Request, res: Response) => {
    const id = Number(req.params.id);
    if (Number.isNaN(id)) return fail(res, "ID inválido", 400);

    const user = await this.getUserById.execute(id);
    if (!user) return fail(res, "Usuario no encontrado", 404);
    return success(res, user, "Usuario encontrado");
  };

  // POST /api/users
  create = async (req: Request, res: Response) => {
    const parsed = CreateUserDto.safeParse(req.body);
    if (!parsed.success) {
      return fail(res, "Datos inválidos", 400, parsed.error.format());
    }

    // Regla de negocio (ya implementada en usecase):
    // - bloquea dominios (ej: @spam.com)
    // - valida email único vía repo.getByEmail
    const created = await this.createUser.execute(parsed.data);
    return success(res, created, "Usuario creado", 201);
  };

  // PUT /api/users/:id
  update = async (req: Request, res: Response) => {
    const id = Number(req.params.id);
    if (Number.isNaN(id)) return fail(res, "ID inválido", 400);
    console.log("olaaa")
    const parsed = UpdateUserDto.safeParse(req.body);
    if (!parsed.success) {
      return fail(res, "Datos inválidos", 400, parsed.error.format());
    }

    const updated = await this.updateUser.execute({ id, ...parsed.data });
    return success(res, updated, "Usuario actualizado");
  };

  // DELETE /api/users/:id
  delete = async (req: Request, res: Response) => {
    const id = Number(req.params.id);
    if (Number.isNaN(id)) return fail(res, "ID inválido", 400);

    await this.deleteUser.execute(id);
    return success(res, { id }, "Usuario eliminado");
  };
}
