import { IRoleRepository } from "../../domain/repositories/IRoleRepository";
export declare class DeleteRole {
    private repo;
    constructor(repo: IRoleRepository);
    execute(id: number): Promise<void>;
}
//# sourceMappingURL=DeleteRole.d.ts.map