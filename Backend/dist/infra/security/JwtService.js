"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.JwtService = void 0;
const jsonwebtoken_1 = __importDefault(require("jsonwebtoken"));
class JwtService {
    constructor() {
        this.secret = (process.env.JWT_SECRET || 'dev-secret-change-me');
        this.issuer = process.env.JWT_ISSUER;
        this.audience = process.env.JWT_AUDIENCE;
        // Acepta "15h" o número en segundos:
        const envExp = process.env.JWT_EXPIRES;
        this.expiresIn =
            !envExp ? '15h'
                : /^\d+$/.test(envExp) ? Number(envExp) // e.g. "54000" (segundos)
                    : envExp; // e.g. "15h"
    }
    sign(payload) {
        const now = Math.floor(Date.now() / 1000);
        const options = {
            expiresIn: this.expiresIn,
            issuer: this.issuer,
            audience: this.audience,
        };
        const token = jsonwebtoken_1.default.sign({ ...payload, iat: now }, this.secret, options);
        const decoded = jsonwebtoken_1.default.decode(token);
        const exp = decoded && typeof decoded.exp === 'number' ? decoded.exp * 1000 : undefined;
        return { token, expiresAt: exp };
    }
    verify(token) {
        return jsonwebtoken_1.default.verify(token, this.secret, {
            issuer: this.issuer,
            audience: this.audience,
        });
    }
}
exports.JwtService = JwtService;
//# sourceMappingURL=JwtService.js.map