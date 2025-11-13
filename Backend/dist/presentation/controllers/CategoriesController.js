"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.CategoriesController = void 0;
const categories_dto_1 = require("../dtos/categories.dto");
const apiResponse_1 = require("../utils/apiResponse");
class CategoriesController {
    constructor(createCategory, listCategories, updateCategoryUC, deleteCategoryUC) {
        this.createCategory = createCategory;
        this.listCategories = listCategories;
        this.updateCategoryUC = updateCategoryUC;
        this.deleteCategoryUC = deleteCategoryUC;
        this.create = async (req, res) => {
            try {
                const dto = categories_dto_1.CreateCategoryDto.parse(req.body);
                const cat = await this.createCategory.exec(dto);
                return (0, apiResponse_1.success)(res, cat, "Created", 201);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error creating category", 400, err);
            }
        };
        this.list = async (_req, res) => {
            try {
                return (0, apiResponse_1.success)(res, await this.listCategories.exec());
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error listing categories", 500, err);
            }
        };
        this.update = async (req, res) => {
            try {
                const id = Number(req.params.id);
                const dto = categories_dto_1.UpdateCategoryDto.parse(req.body);
                const cat = await this.updateCategoryUC.exec(id, dto);
                return (0, apiResponse_1.success)(res, cat, "Updated");
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error updating category", 400, err);
            }
        };
        this.remove = async (req, res) => {
            try {
                const id = Number(req.params.id);
                const hard = req.query.hard === 'true';
                await this.deleteCategoryUC.exec(id, hard);
                return (0, apiResponse_1.success)(res, null, hard ? "Deleted" : "Deactivated", 204);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error deleting category", 400, err);
            }
        };
    }
}
exports.CategoriesController = CategoriesController;
//# sourceMappingURL=CategoriesController.js.map