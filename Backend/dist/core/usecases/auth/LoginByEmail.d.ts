import { IUserRepository } from '../../domain/repositories/IUserRepository';
export declare class LoginByEmail {
    private repo;
    constructor(repo: IUserRepository);
    execute(email: string, password: string): Promise<import("../../domain/entities/User").User>;
}
//# sourceMappingURL=LoginByEmail.d.ts.map