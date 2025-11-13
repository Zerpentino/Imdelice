import bcrypt from 'bcrypt';
import { IUserRepository, CreateUserInput } from '../../domain/repositories/IUserRepository';
import { digestPin, hashPin } from '../../../infra/security/pincode';

export class CreateUser {
  constructor(private repo: IUserRepository) {}

  async execute(
    input: Omit<CreateUserInput, 'passwordHash' | 'pinCodeHash' | 'pinCodeDigest'> & {
      password: string;
      pinCode?: string | null;
    }
  ) {
    // email único
    const exists = await this.repo.getByEmail(input.email);
    if (exists) {
      const e: any = new Error('Email ya está registrado');
      e.status = 409;
      throw e;
    }

    const passwordHash = await bcrypt.hash(input.password, 10);

    let pinCodeHash: string | null = null;
    let pinCodeDigest: string | null = null;

    if (input.pinCode !== undefined) {
      if (input.pinCode === null) {
        // borrar PIN
        pinCodeHash = null;
        pinCodeDigest = null;
      } else {
        // usa digest determinístico para revisar unicidad
        const digest = digestPin(input.pinCode);
        const conflict = await this.repo.getByPinDigest(digest);
        if (conflict) {
          const e: any = new Error('El PIN ya está en uso por otro usuario');
          e.status = 409;
          throw e;
        }
        pinCodeHash = await hashPin(input.pinCode);
        pinCodeDigest = digest;
      }
    }

    return this.repo.create({
      email: input.email,
      name: input.name ?? null,
      roleId: input.roleId,
      passwordHash,
      pinCodeHash,
      pinCodeDigest,
    });
  }
}
