import { Request, Response, NextFunction } from 'express';
export interface AuthRequest extends Request {
    auth?: {
        userId: number;
        email?: string | null;
        roleId?: number;
        raw: any;
    };
}
export declare function authenticate(req: AuthRequest, res: Response, next: NextFunction): void | Response<any, Record<string, any>>;
//# sourceMappingURL=authenticate.d.ts.map