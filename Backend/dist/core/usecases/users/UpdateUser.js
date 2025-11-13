"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateUser = void 0;
const bcrypt_1 = __importDefault(require("bcrypt"));
const pincode_1 = require("../../../infra/security/pincode");
class UpdateUser {
    constructor(repo) {
        this.repo = repo;
    }
    async execute(input) {
        const data = { id: input.id };
        if (input.email !== undefined) {
            data.email = input.email;
            const exists = await this.repo.getByEmail(input.email);
            if (exists) {
                const e = new Error('Email ya está registrado');
                e.status = 409;
                throw e;
            }
        }
        if (input.name !== undefined)
            data.name = input.name;
        if (input.roleId !== undefined)
            data.roleId = input.roleId;
        if (input.password !== undefined) {
            data.passwordHash = await bcrypt_1.default.hash(input.password, 10);
        }
        if (input.pinCode !== undefined) {
            if (input.pinCode === null) {
                // quitar PIN
                data.pinCodeHash = null;
                data.pinCodeDigest = null;
            }
            else {
                const digest = (0, pincode_1.digestPin)(input.pinCode);
                // Verificar que nadie más (distinto al propio usuario) tenga este digest
                const conflict = await this.repo.getByPinDigest(digest);
                if (conflict && conflict.id !== input.id) {
                    const e = new Error('El PIN ya está en uso por otro usuario');
                    e.status = 409;
                    throw e;
                }
                data.pinCodeHash = await (0, pincode_1.hashPin)(input.pinCode);
                data.pinCodeDigest = digest;
            }
        }
        return this.repo.update(data);
    }
}
exports.UpdateUser = UpdateUser;
//# sourceMappingURL=UpdateUser.js.map