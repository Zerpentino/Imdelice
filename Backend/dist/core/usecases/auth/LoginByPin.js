"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.LoginByPin = void 0;
const bcrypt_1 = __importDefault(require("bcrypt"));
class LoginByPin {
    constructor(repo) {
        this.repo = repo;
    }
    async execute(pinCode, password) {
        // 1) Trae lista de usuarios con PIN (puedes paginar si quieres)
        const candidates = await this.repo.list(); // o crea un método repo.listWithPin()
        // 2) Encuentra el primero cuyo bcrypt haga match con el PIN
        const user = await (async () => {
            for (const u of candidates) {
                const hash = u.pinCodeHash;
                if (hash && await bcrypt_1.default.compare(pinCode, hash))
                    return u;
            }
            return null;
        })();
        if (!user) {
            const e = new Error('Credenciales inválidas');
            e.status = 401;
            throw e;
        }
        // 3) Valida password
        const ok = await bcrypt_1.default.compare(password, user.passwordHash);
        if (!ok) {
            const e = new Error('Credenciales inválidas');
            e.status = 401;
            throw e;
        }
        return user;
    }
}
exports.LoginByPin = LoginByPin;
//# sourceMappingURL=LoginByPin.js.map