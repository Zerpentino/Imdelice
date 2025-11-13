import { JwtPayload as StdJwtPayload } from 'jsonwebtoken';
export type AppJwtClaims = {
    sub: string | number;
    email?: string | null;
    roleId?: number;
    perms?: string[];
};
export declare class JwtService {
    private readonly secret;
    private readonly issuer?;
    private readonly audience?;
    private readonly expiresIn;
    constructor();
    sign(payload: AppJwtClaims): {
        token: string;
        expiresAt: number | undefined;
    };
    verify(token: string): StdJwtPayload;
}
//# sourceMappingURL=JwtService.d.ts.map