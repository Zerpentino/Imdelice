import { Router } from 'express';
import { authController } from '../container';
import { asyncHandler } from '../presentation/utils/asyncHandler';
import { authenticate } from '../presentation/middlewares/authenticate'
import { prisma } from '../lib/prisma'

const r = Router();
r.post('/email', asyncHandler(authController.loginEmail));
r.post('/pin', asyncHandler(authController.loginPin));
r.get('/me', authenticate, async (req: any, res) => {
  const user = await prisma.user.findUnique({ where: { id: req.auth.userId }, include: { role: true }})
  const perms = await prisma.permission.findMany({ where: { roles: { some: { roleId: req.auth.roleId }}}})
  return res.json({ ok: true, data: { user, permissions: perms.map(p=>p.code) }})
})
export default r;
