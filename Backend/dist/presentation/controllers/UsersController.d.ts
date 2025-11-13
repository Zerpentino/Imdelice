import { Request, Response } from "express";
import { CreateUser } from "../../core/usecases/users/CreateUser";
import { DeleteUser } from "../../core/usecases/users/DeleteUser";
import { GetUserById } from "../../core/usecases/users/GetUserById";
import { ListUsers } from "../../core/usecases/users/ListUsers";
import { UpdateUser } from "../../core/usecases/users/UpdateUser";
export declare class UsersController {
    private listUsers;
    private getUserById;
    private createUser;
    private updateUser;
    private deleteUser;
    constructor(listUsers: ListUsers, getUserById: GetUserById, createUser: CreateUser, updateUser: UpdateUser, deleteUser: DeleteUser);
    list: (_req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    get: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    create: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    update: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    delete: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
}
//# sourceMappingURL=UsersController.d.ts.map