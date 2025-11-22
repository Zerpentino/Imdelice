"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.requestLogger = void 0;
const replacer = (_key, value) => {
    if (Buffer.isBuffer(value)) {
        return `[Buffer:${value.length}]`;
    }
    return value;
};
const formatBody = (body) => {
    if (body === undefined || body === null)
        return '<empty>';
    if (typeof body !== 'object')
        return String(body);
    if (Buffer.isBuffer(body))
        return `[Buffer:${body.length}]`;
    if (!Object.keys(body).length)
        return '<empty>';
    try {
        return JSON.stringify(body, replacer);
    }
    catch {
        return '<unserializable>';
    }
};
const requestLogger = (req, _res, next) => {
    const bodyPreview = formatBody(req.body);
    console.log(`[HTTP] ${req.method} ${req.originalUrl} body=${bodyPreview}`);
    next();
};
exports.requestLogger = requestLogger;
//# sourceMappingURL=requestLogger.js.map