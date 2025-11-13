import { AuthRequest } from './authenticate';
import { Response, NextFunction } from 'express';
export declare function authorize(...required: string[]): (req: AuthRequest, res: Response, next: NextFunction) => Response<any, Record<string, any>> | undefined;
//# sourceMappingURL=authorize.d.ts.map