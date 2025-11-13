import { Request, Response } from "express";
import { CreateRole } from "../../core/usecases/roles/CreateRole";
import { UpdateRole } from "../../core/usecases/roles/UpdateRole";
import { DeleteRole } from "../../core/usecases/roles/DeleteRole";
import { GetRoleById } from "../../core/usecases/roles/GetRoleById";
import { ListRoles } from "../../core/usecases/roles/ListRoles";
import { SetRolePermissions } from '../../core/usecases/roles/SetRolePermissions';
export declare class RolesController {
    private listRoles;
    private getRoleById;
    private createRole;
    private updateRole;
    private deleteRole;
    private setRolePerms;
    constructor(listRoles: ListRoles, getRoleById: GetRoleById, createRole: CreateRole, updateRole: UpdateRole, deleteRole: DeleteRole, setRolePerms: SetRolePermissions);
    list: (_: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    get: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    create: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    update: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    delete: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
}
//# sourceMappingURL=RolesController.d.ts.map