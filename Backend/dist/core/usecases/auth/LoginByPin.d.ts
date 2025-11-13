import { IUserRepository } from '../../domain/repositories/IUserRepository';
export declare class LoginByPin {
    private repo;
    constructor(repo: IUserRepository);
    execute(pinCode: string, password: string): Promise<import("../../domain/entities/User").User>;
}
//# sourceMappingURL=LoginByPin.d.ts.map