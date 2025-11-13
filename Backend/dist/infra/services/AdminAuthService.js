"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.AdminAuthService = void 0;
const bcrypt_1 = __importDefault(require("bcrypt"));
const prisma_1 = require("../../lib/prisma");
const pincode_1 = require("../security/pincode");
class AdminAuthService {
    async verify(input) {
        if (!input.password) {
            throw new Error("Password requerido");
        }
        let user = null;
        if (input.adminEmail) {
            user = await prisma_1.prisma.user.findUnique({
                where: { email: input.adminEmail },
                include: {
                    role: {
                        include: {
                            permissions: {
                                include: { permission: true },
                            },
                        },
                    },
                },
            });
        }
        else if (input.adminPin) {
            const digest = (0, pincode_1.digestPin)(input.adminPin);
            user = await prisma_1.prisma.user.findFirst({
                where: { pinCodeDigest: digest },
                include: {
                    role: {
                        include: {
                            permissions: {
                                include: { permission: true },
                            },
                        },
                    },
                },
            });
            if (user && user.pinCodeHash) {
                const pinOk = await bcrypt_1.default.compare(input.adminPin, user.pinCodeHash);
                if (!pinOk) {
                    user = null;
                }
            }
            else {
                user = null;
            }
        }
        else {
            throw new Error("Debes enviar adminEmail o adminPin");
        }
        if (!user) {
            const err = new Error("Credenciales inválidas");
            err.status = 401;
            throw err;
        }
        const passwordOk = await bcrypt_1.default.compare(input.password, user.passwordHash);
        if (!passwordOk) {
            const err = new Error("Credenciales inválidas");
            err.status = 401;
            throw err;
        }
        const permissions = user.role.permissions.map((rp) => rp.permission.code);
        if (!permissions.includes("orders.refund")) {
            const err = new Error("Sin permiso para autorizar cancelaciones");
            err.status = 403;
            throw err;
        }
        return user;
    }
}
exports.AdminAuthService = AdminAuthService;
//# sourceMappingURL=AdminAuthService.js.map