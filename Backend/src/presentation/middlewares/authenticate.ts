import { Request, Response, NextFunction } from 'express';
import { JwtService } from '../../infra/security/JwtService';
import { fail } from '../utils/apiResponse';

const jwtService = new JwtService();

export interface AuthRequest extends Request {
  auth?: {
    userId: number;
    email?: string | null;
    roleId?: number;
    raw: any;
  }
}

export function authenticate(req: AuthRequest, res: Response, next: NextFunction) {
  try {
    const auth = req.header('Authorization') || '';
    const match = auth.match(/^Bearer (.+)$/i);
    if (!match) return fail(res, 'No autorizado', 401);

    const payload = jwtService.verify(match[1]);

    req.auth = {
      userId: Number(payload.sub),
      email: (payload as any).email ?? null,
      roleId: (payload as any).roleId ?? undefined,
      raw: payload
    };
    return next();
  } catch (err: any) {
    if (err?.name === 'TokenExpiredError') {
      const expiredAt = err?.expiredAt ? new Date(err.expiredAt).toISOString() : undefined;
      res.set('WWW-Authenticate', 'Bearer error="invalid_token", error_description="token expired"');
      return fail(res, 'Token expirado', 401, { code: 'TOKEN_EXPIRED', expiredAt });
    }
    res.set('WWW-Authenticate', 'Bearer error="invalid_token", error_description="invalid signature or malformed"');
    return fail(res, 'Token inválido', 401, { code: 'TOKEN_INVALID' });
  }
}
