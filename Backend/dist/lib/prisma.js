"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.prisma = void 0;
// src/lib/prisma.ts
const client_1 = require("@prisma/client");
const globalForPrisma = global;
const prismaLogLevels = ['warn', 'error'];
if (process.env.DEBUG_PRISMA_QUERIES === 'true') {
    prismaLogLevels.unshift('query');
}
exports.prisma = globalForPrisma.prisma ||
    new client_1.PrismaClient({
        log: prismaLogLevels,
    });
if (process.env.NODE_ENV !== 'production')
    globalForPrisma.prisma = exports.prisma;
//# sourceMappingURL=prisma.js.map