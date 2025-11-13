import { IRoleRepository } from "../../domain/repositories/IRoleRepository";
export declare class CreateRole {
    private repo;
    constructor(repo: IRoleRepository);
    execute(input: {
        name: string;
        description?: string | null;
    }): Promise<import("../../domain/entities/Role").Role>;
}
//# sourceMappingURL=CreateRole.d.ts.map