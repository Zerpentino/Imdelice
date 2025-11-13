"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.authorize = authorize;
const apiResponse_1 = require("../utils/apiResponse");
function authorize(...required) {
    return (req, res, next) => {
        const perms = (req.auth?.raw?.perms ?? []);
        const ok = required.every(r => perms.includes(r)); // exige TODOS
        if (!ok)
            return (0, apiResponse_1.fail)(res, 'Prohibido: falta permiso', 403, { required, have: perms });
        next();
    };
}
//# sourceMappingURL=authorize.js.map