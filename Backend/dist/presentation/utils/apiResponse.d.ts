import { Response } from "express";
export declare function success(res: Response, data: any, message?: string, status?: number): Response<any, Record<string, any>>;
export declare function fail(res: Response, message?: string, status?: number, details?: any): Response<any, Record<string, any>>;
//# sourceMappingURL=apiResponse.d.ts.map