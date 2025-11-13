import { IUserRepository, CreateUserInput } from '../../domain/repositories/IUserRepository';
export declare class CreateUser {
    private repo;
    constructor(repo: IUserRepository);
    execute(input: Omit<CreateUserInput, 'passwordHash' | 'pinCodeHash' | 'pinCodeDigest'> & {
        password: string;
        pinCode?: string | null;
    }): Promise<import("../../domain/entities/User").User>;
}
//# sourceMappingURL=CreateUser.d.ts.map