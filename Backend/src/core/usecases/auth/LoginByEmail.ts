import bcrypt from 'bcrypt';
import { IUserRepository } from '../../domain/repositories/IUserRepository';

export class LoginByEmail {
  constructor(private repo: IUserRepository) {}
  async execute(email: string, password: string) {
    const user = await this.repo.getByEmail(email);
    if (!user) {
      const e: any = new Error('Credenciales inválidas');
      e.status = 401;
      throw e;
    }
    const ok = await bcrypt.compare(password, (user as any).passwordHash);
    if (!ok) {
      const e: any = new Error('Credenciales inválidas');
      e.status = 401;
      throw e;
    }
    return user; // aquí normalmente generas JWT en capa de presentación/servicio
  }
}
