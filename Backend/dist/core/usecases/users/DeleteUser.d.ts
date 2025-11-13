import { IUserRepository } from '../../domain/repositories/IUserRepository';
export declare class DeleteUser {
    private repo;
    constructor(repo: IUserRepository);
    execute(id: number): Promise<void>;
}
//# sourceMappingURL=DeleteUser.d.ts.map