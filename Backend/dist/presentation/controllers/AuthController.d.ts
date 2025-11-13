import { Request, Response } from 'express';
import { LoginByEmail } from '../../core/usecases/auth/LoginByEmail';
import { LoginByPin } from '../../core/usecases/auth/LoginByPin';
import { JwtService } from '../../infra/security/JwtService';
import { GetPermissionsByRole } from '../../core/usecases/roles/GetPermissionsByRole';
export declare class AuthController {
    private loginByEmail;
    private loginByPin;
    private jwt;
    private getPermsByRole;
    constructor(loginByEmail: LoginByEmail, loginByPin: LoginByPin, jwt: JwtService, getPermsByRole: GetPermissionsByRole);
    loginEmail: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    loginPin: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
}
//# sourceMappingURL=AuthController.d.ts.map