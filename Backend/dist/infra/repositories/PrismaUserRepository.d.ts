import { IUserRepository, CreateUserInput, UpdateUserInput } from '../../core/domain/repositories/IUserRepository';
import { User } from '../../core/domain/entities/User';
export declare class PrismaUserRepository implements IUserRepository {
    list(): Promise<User[]>;
    getById(id: number): Promise<User | null>;
    getByEmail(email: string): Promise<User | null>;
    getByPinHash(pinHash: string): Promise<User | null>;
    create(input: CreateUserInput): Promise<User>;
    update(input: UpdateUserInput): Promise<User>;
    getByPinDigest(digest: string): Promise<User | null>;
    delete(id: number): Promise<void>;
}
//# sourceMappingURL=PrismaUserRepository.d.ts.map