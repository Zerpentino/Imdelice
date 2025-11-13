import { Request, Response } from 'express';
import { success, fail } from '../utils/apiResponse';
import { LoginByEmail } from '../../core/usecases/auth/LoginByEmail';
import { LoginByPin } from '../../core/usecases/auth/LoginByPin';
import { LoginByEmailDto, LoginByPinDto } from '../dtos/auth.dto';
import { JwtService } from '../../infra/security/JwtService';
import { GetPermissionsByRole } from '../../core/usecases/roles/GetPermissionsByRole'

// (Opcional) Generar JWT aquí si lo necesitas
export class AuthController {
    constructor(
    private loginByEmail: LoginByEmail,
    private loginByPin: LoginByPin,
    private jwt: JwtService,
        private getPermsByRole: GetPermissionsByRole // 👈 inyecta por container

  ) {}

   loginEmail = async (req: Request, res: Response) => {
    const parsed = LoginByEmailDto.safeParse(req.body);
    if (!parsed.success) return fail(res, 'Datos inválidos', 400, parsed.error.format());

    const user = await this.loginByEmail.execute(parsed.data.email, parsed.data.password);
    const perms = await this.getPermsByRole.execute((user as any).roleId)

    const { token, expiresAt } = this.jwt.sign({
      sub: (user as any).id,
      email: (user as any).email ?? null,
      roleId: (user as any).roleId,
            perms: perms.map(p => p.code) // 👈

    });

    return success(res, { user, token, expiresAt }, 'Login OK');
  };

  loginPin = async (req: Request, res: Response) => {
    const parsed = LoginByPinDto.safeParse(req.body);
    if (!parsed.success) return fail(res, 'Datos inválidos', 400, parsed.error.format());

    const user = await this.loginByPin.execute(parsed.data.pinCode, parsed.data.password);
    const perms = await this.getPermsByRole.execute((user as any).roleId)

    const { token, expiresAt } = this.jwt.sign({
      sub: (user as any).id,
      email: (user as any).email ?? null,
      roleId: (user as any).roleId,
            perms: perms.map(p => p.code)

    });

    return success(res, { user, token, expiresAt }, 'Login OK');
  };
}