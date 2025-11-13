"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.AuthController = void 0;
const apiResponse_1 = require("../utils/apiResponse");
const auth_dto_1 = require("../dtos/auth.dto");
// (Opcional) Generar JWT aquí si lo necesitas
class AuthController {
    constructor(loginByEmail, loginByPin, jwt, getPermsByRole // 👈 inyecta por container
    ) {
        this.loginByEmail = loginByEmail;
        this.loginByPin = loginByPin;
        this.jwt = jwt;
        this.getPermsByRole = getPermsByRole;
        this.loginEmail = async (req, res) => {
            const parsed = auth_dto_1.LoginByEmailDto.safeParse(req.body);
            if (!parsed.success)
                return (0, apiResponse_1.fail)(res, 'Datos inválidos', 400, parsed.error.format());
            const user = await this.loginByEmail.execute(parsed.data.email, parsed.data.password);
            const perms = await this.getPermsByRole.execute(user.roleId);
            const { token, expiresAt } = this.jwt.sign({
                sub: user.id,
                email: user.email ?? null,
                roleId: user.roleId,
                perms: perms.map(p => p.code) // 👈
            });
            return (0, apiResponse_1.success)(res, { user, token, expiresAt }, 'Login OK');
        };
        this.loginPin = async (req, res) => {
            const parsed = auth_dto_1.LoginByPinDto.safeParse(req.body);
            if (!parsed.success)
                return (0, apiResponse_1.fail)(res, 'Datos inválidos', 400, parsed.error.format());
            const user = await this.loginByPin.execute(parsed.data.pinCode, parsed.data.password);
            const perms = await this.getPermsByRole.execute(user.roleId);
            const { token, expiresAt } = this.jwt.sign({
                sub: user.id,
                email: user.email ?? null,
                roleId: user.roleId,
                perms: perms.map(p => p.code)
            });
            return (0, apiResponse_1.success)(res, { user, token, expiresAt }, 'Login OK');
        };
    }
}
exports.AuthController = AuthController;
//# sourceMappingURL=AuthController.js.map