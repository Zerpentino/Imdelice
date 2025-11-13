"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.authenticate = authenticate;
const JwtService_1 = require("../../infra/security/JwtService");
const apiResponse_1 = require("../utils/apiResponse");
const jwtService = new JwtService_1.JwtService();
function authenticate(req, res, next) {
    try {
        const auth = req.header('Authorization') || '';
        const match = auth.match(/^Bearer (.+)$/i);
        if (!match)
            return (0, apiResponse_1.fail)(res, 'No autorizado', 401);
        const payload = jwtService.verify(match[1]);
        req.auth = {
            userId: Number(payload.sub),
            email: payload.email ?? null,
            roleId: payload.roleId ?? undefined,
            raw: payload
        };
        return next();
    }
    catch (err) {
        if (err?.name === 'TokenExpiredError') {
            const expiredAt = err?.expiredAt ? new Date(err.expiredAt).toISOString() : undefined;
            res.set('WWW-Authenticate', 'Bearer error="invalid_token", error_description="token expired"');
            return (0, apiResponse_1.fail)(res, 'Token expirado', 401, { code: 'TOKEN_EXPIRED', expiredAt });
        }
        res.set('WWW-Authenticate', 'Bearer error="invalid_token", error_description="invalid signature or malformed"');
        return (0, apiResponse_1.fail)(res, 'Token inválido', 401, { code: 'TOKEN_INVALID' });
    }
}
//# sourceMappingURL=authenticate.js.map