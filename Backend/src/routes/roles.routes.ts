import { Router } from 'express';
import { rolesController } from '../container';
import { asyncHandler } from '../presentation/utils/asyncHandler';
import { authenticate } from "../presentation/middlewares/authenticate";
import { authorize } from '../presentation/middlewares/authorize'

const r = Router();
// protege todas las rutas de /users
r.use(authenticate);

r.get('/:id', authorize('roles.read'),asyncHandler(rolesController.get));

r.get('/', authorize('roles.read'), asyncHandler(rolesController.list))
r.post('/', authorize('roles.create'), asyncHandler(rolesController.create))
r.put('/:id', authorize('roles.update'), asyncHandler(rolesController.update))
r.delete('/:id', authorize('roles.delete'), asyncHandler(rolesController.delete))

export default r;
