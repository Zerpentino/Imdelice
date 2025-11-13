import { prisma } from '../../lib/prisma';
import {
  IUserRepository,
  CreateUserInput,
  UpdateUserInput,
} from '../../core/domain/repositories/IUserRepository';
import { User } from '../../core/domain/entities/User';

export class PrismaUserRepository implements IUserRepository {
  async list(): Promise<User[]> {
    // Si no necesitas el role eager-loaded, quita include
    return prisma.user.findMany({ include: { role: true } }) as unknown as User[];
  }

   async getById(id: number): Promise<User | null> {
    return (await prisma.user.findUnique({
      where: { id },
      include: { role: true },
    })) as unknown as User | null;
  }
  async getByEmail(email: string): Promise<User | null> {
    return (await prisma.user.findUnique({
      where: { email },
      include: { role: true },
    })) as unknown as User | null;
  }
  async getByPinHash(pinHash: string): Promise<User | null> {
    // Ajusta el nombre del campo si en Prisma el atributo es distinto (p.ej. pin_hash)
    return (await prisma.user.findFirst({
      where: { pinCodeHash: pinHash },
      include: { role: true },
    })) as unknown as User | null;
  }
async create(input: CreateUserInput): Promise<User> {
  return (await prisma.user.create({
    data: {
      email: input.email,
      name: input.name ?? null,
      roleId: input.roleId,
      passwordHash: input.passwordHash,
      pinCodeHash: input.pinCodeHash ?? null,
      pinCodeDigest: (input as any).pinCodeDigest ?? null,  // 👈 FIX
    },
    include: { role: true },
  })) as unknown as User;
}


async update(input: UpdateUserInput): Promise<User> {
  const { id, ...data } = input;

  return (await prisma.user.update({
    where: { id },
    data: {
      ...(data.email !== undefined ? { email: data.email } : {}),
      ...(data.name !== undefined ? { name: data.name } : {}),
      ...(data.roleId !== undefined ? { roleId: data.roleId } : {}),
      ...(data.passwordHash !== undefined ? { passwordHash: data.passwordHash } : {}),
      ...(data.pinCodeHash !== undefined ? { pinCodeHash: data.pinCodeHash } : {}),
      ...((data as any).pinCodeDigest !== undefined
        ? { pinCodeDigest: (data as any).pinCodeDigest }
        : {}),
    },
    include: { role: true },
  })) as unknown as User;
}
   async getByPinDigest(digest: string) {
    return prisma.user.findFirst({
      where: { pinCodeDigest: digest },
      include: { role: true },
    }) as unknown as User | null;
  }

  // ✅ ERROR 1308/2355: esta función debe ser async si usas await
  async delete(id: number): Promise<void> {
    await prisma.user.delete({ where: { id } });
  }


}
