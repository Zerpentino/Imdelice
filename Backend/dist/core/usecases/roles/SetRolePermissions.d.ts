import { IPermissionRepository } from '../../domain/repositories/IPermissionRepository';
export declare class SetRolePermissions {
    private repo;
    constructor(repo: IPermissionRepository);
    execute(roleId: number, codes: string[]): Promise<void>;
}
//# sourceMappingURL=SetRolePermissions.d.ts.map