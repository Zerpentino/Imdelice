import { IPermissionRepository } from '../../domain/repositories/IPermissionRepository';
export declare class GetPermissionsByRole {
    private repo;
    constructor(repo: IPermissionRepository);
    execute(roleId: number): Promise<import("../../domain/entities/Permission").Permission[]>;
}
//# sourceMappingURL=GetPermissionsByRole.d.ts.map