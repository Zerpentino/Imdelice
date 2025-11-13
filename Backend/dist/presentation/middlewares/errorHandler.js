"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.errorHandler = errorHandler;
const apiResponse_1 = require("../utils/apiResponse");
function errorHandler(err, _req, res, _next) {
    console.error(err); // log real
    const status = err.status || 500;
    const message = err.message || "Error interno";
    return (0, apiResponse_1.fail)(res, message, status, err.details);
}
//# sourceMappingURL=errorHandler.js.map