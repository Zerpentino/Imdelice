import { IRoleRepository } from "../../domain/repositories/IRoleRepository";
export declare class ListRoles {
    private repo;
    constructor(repo: IRoleRepository);
    execute(): Promise<import("../../domain/entities/Role").Role[]>;
}
//# sourceMappingURL=ListRoles.d.ts.map