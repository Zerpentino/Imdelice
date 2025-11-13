import jwt, { Secret, SignOptions, JwtPayload as StdJwtPayload } from 'jsonwebtoken';

export type AppJwtClaims = {
  sub: string | number;     // user id
  email?: string | null;
  roleId?: number;
    perms?: string[]        // 👈

};

export class JwtService {
  private readonly secret: Secret;
  private readonly issuer?: string;
  private readonly audience?: string;
  private readonly expiresIn: SignOptions['expiresIn'];

  constructor() {
    this.secret   = (process.env.JWT_SECRET || 'dev-secret-change-me') as Secret;
    this.issuer   = process.env.JWT_ISSUER;
    this.audience = process.env.JWT_AUDIENCE;

    // Acepta "15h" o número en segundos:
    const envExp = process.env.JWT_EXPIRES;
    this.expiresIn =
      !envExp ? '15h'
      : /^\d+$/.test(envExp) ? Number(envExp)   // e.g. "54000" (segundos)
      : (envExp as SignOptions['expiresIn']);   // e.g. "15h"
  }

  sign(payload: AppJwtClaims) {
    const now = Math.floor(Date.now() / 1000);

    const options: SignOptions = {
      expiresIn: this.expiresIn,
      issuer: this.issuer,
      audience: this.audience,
    };

    const token = jwt.sign({ ...payload, iat: now }, this.secret, options);

    const decoded = jwt.decode(token) as StdJwtPayload | null;
    const exp = decoded && typeof decoded.exp === 'number' ? decoded.exp * 1000 : undefined;

    return { token, expiresAt: exp };
  }

  verify(token: string) {
    return jwt.verify(token, this.secret, {
      issuer: this.issuer,
      audience: this.audience,
    }) as StdJwtPayload;
  }
}
