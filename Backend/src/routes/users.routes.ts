import { Router } from "express";
import { usersController } from "../container";
import { asyncHandler } from "../presentation/utils/asyncHandler";
import { authenticate } from "../presentation/middlewares/authenticate";
import { authorize } from '../presentation/middlewares/authorize'

const router = Router();

// protege todas las rutas de /users
router.use(authenticate);

router.get('/', authorize('users.read'), asyncHandler(usersController.list))
router.post('/', authorize('users.create'), asyncHandler(usersController.create))
router.put('/:id', authorize('users.update'), asyncHandler(usersController.update))
router.delete('/:id', authorize('users.delete'), asyncHandler(usersController.delete))

export default router;
