import { User } from '../entities/User';
export interface CreateUserInput {
  email: string;
  name?: string | null;
  roleId: number;
  passwordHash: string;
  pinCodeHash?: string | null;
    pinCodeDigest?: string | null;   // 👈 NUEVO

}

export interface UpdateUserInput {
  id: number;
  email?: string;
  name?: string | null;
  roleId?: number;
  passwordHash?: string;
  pinCodeHash?: string | null;
    pinCodeDigest?: string | null;   // 👈 NUEVO

}

export interface IUserRepository {
  list(): Promise<User[]>;
  getById(id: number): Promise<User | null>;
  getByEmail(email: string): Promise<User | null>;
  // 👇 nuevo
  getByPinHash(pinHash: string): Promise<User | null>;
  getByPinDigest(digest: string): Promise<User | null>;  // 👈 nuevo

  create(input: CreateUserInput): Promise<User>;
  update(input: UpdateUserInput): Promise<User>;
  delete(id: number): Promise<void>;
}
