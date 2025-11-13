import { Request, Response } from "express";
import { CreateCategory } from "../../core/usecases/categories/CreateCategory";
import { ListCategories } from "../../core/usecases/categories/ListCategories";
import { UpdateCategory } from "../../core/usecases/categories/UpdateCategory";
import { DeleteCategory } from "../../core/usecases/categories/DeleteCategory";
import { CreateCategoryDto, UpdateCategoryDto } from "../dtos/categories.dto";
import { success, fail } from "../utils/apiResponse";

export class CategoriesController {
  constructor(
    private createCategory: CreateCategory,
    private listCategories: ListCategories,
    private updateCategoryUC: UpdateCategory,
    private deleteCategoryUC: DeleteCategory
  ) {}

  create = async (req: Request, res: Response) => {
    try {
      const dto = CreateCategoryDto.parse(req.body);
      const cat = await this.createCategory.exec(dto);
      return success(res, cat, "Created", 201);
    } catch (err: any) { return fail(res, err?.message || "Error creating category", 400, err); }
  };

  list = async (_req: Request, res: Response) => {
    try { return success(res, await this.listCategories.exec()); }
    catch (err: any) { return fail(res, err?.message || "Error listing categories", 500, err); }
  };

  update = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      const dto = UpdateCategoryDto.parse(req.body);
      const cat = await this.updateCategoryUC.exec(id, dto);
      return success(res, cat, "Updated");
    } catch (err: any) { return fail(res, err?.message || "Error updating category", 400, err); }
  };

  remove = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      const hard = req.query.hard === 'true';
      await this.deleteCategoryUC.exec(id, hard);
      return success(res, null, hard ? "Deleted" : "Deactivated", 204);
    } catch (err: any) { return fail(res, err?.message || "Error deleting category", 400, err); }
  };
}
