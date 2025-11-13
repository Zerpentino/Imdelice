import { IUserRepository } from '../../domain/repositories/IUserRepository';
export declare class GetUserById {
    private repo;
    constructor(repo: IUserRepository);
    execute(id: number): Promise<import("../../domain/entities/User").User | null>;
}
//# sourceMappingURL=GetUserById.d.ts.map