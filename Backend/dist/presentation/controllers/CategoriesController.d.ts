import { Request, Response } from "express";
import { CreateCategory } from "../../core/usecases/categories/CreateCategory";
import { ListCategories } from "../../core/usecases/categories/ListCategories";
import { UpdateCategory } from "../../core/usecases/categories/UpdateCategory";
import { DeleteCategory } from "../../core/usecases/categories/DeleteCategory";
export declare class CategoriesController {
    private createCategory;
    private listCategories;
    private updateCategoryUC;
    private deleteCategoryUC;
    constructor(createCategory: CreateCategory, listCategories: ListCategories, updateCategoryUC: UpdateCategory, deleteCategoryUC: DeleteCategory);
    create: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    list: (_req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    update: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    remove: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
}
//# sourceMappingURL=CategoriesController.d.ts.map