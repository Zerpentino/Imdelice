import { IPermissionRepository } from '../../core/domain/repositories/IPermissionRepository';
import { Permission } from '../../core/domain/entities/Permission';
export declare class PrismaPermissionRepository implements IPermissionRepository {
    list(): Promise<Permission[]>;
    getByCodes(codes: string[]): Promise<Permission[]>;
    listByRole(roleId: number): Promise<Permission[]>;
    replaceRolePermissions(roleId: number, codes: string[]): Promise<void>;
}
//# sourceMappingURL=PrismaPermissionRepository.d.ts.map