import { IUserRepository } from "../../domain/repositories/IUserRepository";
export declare class ListUsers {
    private repo;
    constructor(repo: IUserRepository);
    execute(): Promise<import("../../domain/entities/User").User[]>;
}
//# sourceMappingURL=ListUsers.d.ts.map