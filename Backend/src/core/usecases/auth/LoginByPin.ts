import bcrypt from 'bcrypt';
import { IUserRepository } from '../../domain/repositories/IUserRepository';

export class LoginByPin {
  constructor(private repo: IUserRepository) {}

  async execute(pinCode: string, password: string) {
    // 1) Trae lista de usuarios con PIN (puedes paginar si quieres)
    const candidates = await this.repo.list(); // o crea un método repo.listWithPin()

    // 2) Encuentra el primero cuyo bcrypt haga match con el PIN
    const user = await (async () => {
      for (const u of candidates) {
        const hash = (u as any).pinCodeHash as string | null;
        if (hash && await bcrypt.compare(pinCode, hash)) return u;
      }
      return null;
    })();

    if (!user) {
      const e: any = new Error('Credenciales inválidas');
      e.status = 401;
      throw e;
    }

    // 3) Valida password
    const ok = await bcrypt.compare(password, (user as any).passwordHash);
    if (!ok) {
      const e: any = new Error('Credenciales inválidas');
      e.status = 401;
      throw e;
    }
    return user;
  }
}
