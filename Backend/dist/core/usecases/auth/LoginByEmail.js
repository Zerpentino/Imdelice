"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.LoginByEmail = void 0;
const bcrypt_1 = __importDefault(require("bcrypt"));
class LoginByEmail {
    constructor(repo) {
        this.repo = repo;
    }
    async execute(email, password) {
        const user = await this.repo.getByEmail(email);
        if (!user) {
            const e = new Error('Credenciales inválidas');
            e.status = 401;
            throw e;
        }
        const ok = await bcrypt_1.default.compare(password, user.passwordHash);
        if (!ok) {
            const e = new Error('Credenciales inválidas');
            e.status = 401;
            throw e;
        }
        return user; // aquí normalmente generas JWT en capa de presentación/servicio
    }
}
exports.LoginByEmail = LoginByEmail;
//# sourceMappingURL=LoginByEmail.js.map