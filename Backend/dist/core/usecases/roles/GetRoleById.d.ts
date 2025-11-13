import { IRoleRepository } from "../../domain/repositories/IRoleRepository";
export declare class GetRoleById {
    private repo;
    constructor(repo: IRoleRepository);
    execute(id: number): Promise<import("../../domain/repositories/IRoleRepository").RoleWithPermissions | null>;
}
//# sourceMappingURL=GetRoleById.d.ts.map