import { IRoleRepository, CreateRoleInput, UpdateRoleInput, RoleWithPermissions } from "../../core/domain/repositories/IRoleRepository";
import { Role } from "../../core/domain/entities/Role";
export declare class PrismaRoleRepository implements IRoleRepository {
    list(): Promise<Role[]>;
    getById(id: number): Promise<RoleWithPermissions | null>;
    getByName(name: string): Promise<Role | null>;
    create(input: CreateRoleInput): Promise<Role>;
    update(input: UpdateRoleInput): Promise<Role>;
    delete(id: number): Promise<void>;
}
//# sourceMappingURL=PrismaRoleRepository.d.ts.map