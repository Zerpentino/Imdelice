import { Router } from 'express';
import { asyncHandler } from '../presentation/utils/asyncHandler';
import { authenticate } from '../presentation/middlewares/authenticate';
import { authorize } from '../presentation/middlewares/authorize';
import { tablesController } from '../container';

const router = Router();

router.use(authenticate);

router.post('/', authorize('tables.create'), asyncHandler(tablesController.create));
router.get('/', authorize('tables.read'), asyncHandler(tablesController.list));
router.get('/:id', authorize('tables.read'), asyncHandler(tablesController.getOne));
router.patch('/:id', authorize('tables.update'), asyncHandler(tablesController.update));
router.delete('/:id', authorize('tables.delete'), asyncHandler(tablesController.remove));

export default router;
