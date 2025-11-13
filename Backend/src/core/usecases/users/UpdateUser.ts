import bcrypt from 'bcrypt';
import { IUserRepository, UpdateUserInput } from '../../domain/repositories/IUserRepository';
import { digestPin, hashPin } from '../../../infra/security/pincode';

export class UpdateUser {
  constructor(private repo: IUserRepository) {}
  async execute(input: UpdateUserInput & { password?: string; pinCode?: string | null }) {
    const data: UpdateUserInput & { pinCodeDigest?: string | null } = { id: input.id };

    if (input.email !== undefined)
       {
        data.email = input.email;
        const exists = await this.repo.getByEmail(input.email);
         if (exists) {
                const e: any = new Error('Email ya está registrado');
            e.status = 409;
            throw e;
          }
       }


    

    if (input.name !== undefined) data.name = input.name;
    if (input.roleId !== undefined) data.roleId = input.roleId;

    if (input.password !== undefined) {
      data.passwordHash = await bcrypt.hash(input.password, 10);
    }
  
    if (input.pinCode !== undefined) {
      if (input.pinCode === null) {
        // quitar PIN
        data.pinCodeHash = null;
        data.pinCodeDigest = null;
      } else {
        const digest = digestPin(input.pinCode);
        // Verificar que nadie más (distinto al propio usuario) tenga este digest
        const conflict = await this.repo.getByPinDigest(digest);
        if (conflict && (conflict as any).id !== input.id) {
          const e: any = new Error('El PIN ya está en uso por otro usuario');
          e.status = 409;
          throw e;
        }
        data.pinCodeHash = await hashPin(input.pinCode);
        data.pinCodeDigest = digest;
      }
    }

    return this.repo.update(data);
  }
}
