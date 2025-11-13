"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.success = success;
exports.fail = fail;
function success(res, data, message = "OK", status = 200) {
    return res.status(status).json({
        error: null,
        data,
        message,
    });
}
function fail(res, message = "Error", status = 500, details) {
    return res.status(status).json({
        error: { code: status, details },
        data: null,
        message,
    });
}
//# sourceMappingURL=apiResponse.js.map