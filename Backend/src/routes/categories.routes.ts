import { Router } from "express";
import { asyncHandler } from "../presentation/utils/asyncHandler";
import { authenticate } from "../presentation/middlewares/authenticate";
import { categoriesController } from "../container";
import { authorize } from '../presentation/middlewares/authorize'

const router = Router();
router.use(authenticate);



router.post('/', authorize('categories.create'), asyncHandler(categoriesController.create))
router.get('/', authorize('categories.read'), asyncHandler(categoriesController.list))
router.patch('/:id', authorize('categories.update'), asyncHandler(categoriesController.update))
router.delete('/:id', authorize('categories.delete'), asyncHandler(categoriesController.remove))

export default router;
