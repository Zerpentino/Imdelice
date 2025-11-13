import { IRoleRepository } from "../../domain/repositories/IRoleRepository";
export declare class UpdateRole {
    private repo;
    constructor(repo: IRoleRepository);
    execute(input: {
        id: number;
        name?: string;
        description?: string | null;
    }): Promise<import("../../domain/entities/Role").Role>;
}
//# sourceMappingURL=UpdateRole.d.ts.map