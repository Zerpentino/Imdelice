"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const express_1 = require("express");
const container_1 = require("../container");
const asyncHandler_1 = require("../presentation/utils/asyncHandler");
const authenticate_1 = require("../presentation/middlewares/authenticate");
const prisma_1 = require("../lib/prisma");
const r = (0, express_1.Router)();
r.post('/email', (0, asyncHandler_1.asyncHandler)(container_1.authController.loginEmail));
r.post('/pin', (0, asyncHandler_1.asyncHandler)(container_1.authController.loginPin));
r.get('/me', authenticate_1.authenticate, async (req, res) => {
    const user = await prisma_1.prisma.user.findUnique({ where: { id: req.auth.userId }, include: { role: true } });
    const perms = await prisma_1.prisma.permission.findMany({ where: { roles: { some: { roleId: req.auth.roleId } } } });
    return res.json({ ok: true, data: { user, permissions: perms.map(p => p.code) } });
});
exports.default = r;
//# sourceMappingURL=auth.routes.js.map