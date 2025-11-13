"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.PrismaUserRepository = void 0;
const prisma_1 = require("../../lib/prisma");
class PrismaUserRepository {
    async list() {
        // Si no necesitas el role eager-loaded, quita include
        return prisma_1.prisma.user.findMany({ include: { role: true } });
    }
    async getById(id) {
        return (await prisma_1.prisma.user.findUnique({
            where: { id },
            include: { role: true },
        }));
    }
    async getByEmail(email) {
        return (await prisma_1.prisma.user.findUnique({
            where: { email },
            include: { role: true },
        }));
    }
    async getByPinHash(pinHash) {
        // Ajusta el nombre del campo si en Prisma el atributo es distinto (p.ej. pin_hash)
        return (await prisma_1.prisma.user.findFirst({
            where: { pinCodeHash: pinHash },
            include: { role: true },
        }));
    }
    async create(input) {
        return (await prisma_1.prisma.user.create({
            data: {
                email: input.email,
                name: input.name ?? null,
                roleId: input.roleId,
                passwordHash: input.passwordHash,
                pinCodeHash: input.pinCodeHash ?? null,
                pinCodeDigest: input.pinCodeDigest ?? null, // 👈 FIX
            },
            include: { role: true },
        }));
    }
    async update(input) {
        const { id, ...data } = input;
        return (await prisma_1.prisma.user.update({
            where: { id },
            data: {
                ...(data.email !== undefined ? { email: data.email } : {}),
                ...(data.name !== undefined ? { name: data.name } : {}),
                ...(data.roleId !== undefined ? { roleId: data.roleId } : {}),
                ...(data.passwordHash !== undefined ? { passwordHash: data.passwordHash } : {}),
                ...(data.pinCodeHash !== undefined ? { pinCodeHash: data.pinCodeHash } : {}),
                ...(data.pinCodeDigest !== undefined
                    ? { pinCodeDigest: data.pinCodeDigest }
                    : {}),
            },
            include: { role: true },
        }));
    }
    async getByPinDigest(digest) {
        return prisma_1.prisma.user.findFirst({
            where: { pinCodeDigest: digest },
            include: { role: true },
        });
    }
    // ✅ ERROR 1308/2355: esta función debe ser async si usas await
    async delete(id) {
        await prisma_1.prisma.user.delete({ where: { id } });
    }
}
exports.PrismaUserRepository = PrismaUserRepository;
//# sourceMappingURL=PrismaUserRepository.js.map