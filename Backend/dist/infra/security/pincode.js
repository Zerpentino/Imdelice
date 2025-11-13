"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.hashPin = hashPin;
exports.digestPin = digestPin;
// src/infra/security/pincode.ts
const crypto_1 = __importDefault(require("crypto"));
const bcrypt_1 = __importDefault(require("bcrypt"));
const PEPPER = process.env.PINCODE_PEPPER || 'dev-pepper-change-me';
async function hashPin(pin) {
    return bcrypt_1.default.hash(pin, 10);
}
function digestPin(pin) {
    return crypto_1.default.createHmac('sha256', PEPPER).update(pin, 'utf8').digest('hex');
}
//# sourceMappingURL=pincode.js.map