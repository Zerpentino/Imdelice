// src/presentation/middlewares/authorize.ts
import { AuthRequest } from './authenticate'
import { Response, NextFunction } from 'express'
import { fail } from '../utils/apiResponse'

export function authorize(...required: string[]) {
  return (req: AuthRequest, res: Response, next: NextFunction) => {
    const perms = (req.auth?.raw?.perms ?? []) as string[]
    const ok = required.every(r => perms.includes(r)) // exige TODOS
    if (!ok) return fail(res, 'Prohibido: falta permiso', 403, { required, have: perms })
    next()
  }
}
