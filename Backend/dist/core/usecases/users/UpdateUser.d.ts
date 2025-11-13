import { IUserRepository, UpdateUserInput } from '../../domain/repositories/IUserRepository';
export declare class UpdateUser {
    private repo;
    constructor(repo: IUserRepository);
    execute(input: UpdateUserInput & {
        password?: string;
        pinCode?: string | null;
    }): Promise<import("../../domain/entities/User").User>;
}
//# sourceMappingURL=UpdateUser.d.ts.map