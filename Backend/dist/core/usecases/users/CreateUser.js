"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.CreateUser = void 0;
const bcrypt_1 = __importDefault(require("bcrypt"));
const pincode_1 = require("../../../infra/security/pincode");
class CreateUser {
    constructor(repo) {
        this.repo = repo;
    }
    async execute(input) {
        // email único
        const exists = await this.repo.getByEmail(input.email);
        if (exists) {
            const e = new Error('Email ya está registrado');
            e.status = 409;
            throw e;
        }
        const passwordHash = await bcrypt_1.default.hash(input.password, 10);
        let pinCodeHash = null;
        let pinCodeDigest = null;
        if (input.pinCode !== undefined) {
            if (input.pinCode === null) {
                // borrar PIN
                pinCodeHash = null;
                pinCodeDigest = null;
            }
            else {
                // usa digest determinístico para revisar unicidad
                const digest = (0, pincode_1.digestPin)(input.pinCode);
                const conflict = await this.repo.getByPinDigest(digest);
                if (conflict) {
                    const e = new Error('El PIN ya está en uso por otro usuario');
                    e.status = 409;
                    throw e;
                }
                pinCodeHash = await (0, pincode_1.hashPin)(input.pinCode);
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
exports.CreateUser = CreateUser;
//# sourceMappingURL=CreateUser.js.map