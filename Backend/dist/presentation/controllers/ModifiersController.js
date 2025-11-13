"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ModifiersController = void 0;
const modifiers_dto_1 = require("../dtos/modifiers.dto");
const apiResponse_1 = require("../utils/apiResponse");
const modifiers_dto_2 = require("../dtos/modifiers.dto");
const modifiers_dto_3 = require("../dtos/modifiers.dto");
const modifiers_dto_4 = require("../dtos/modifiers.dto");
const modifiers_dto_5 = require("../dtos/modifiers.dto");
class ModifiersController {
    constructor(createGroupUC, updateGroupUC, replaceOptionsUC, deleteGroupUC, listGroupsUC, // <—
    getGroupUC, // <—
    listByProductUC, listProductsByGroupUC, // 👈 NUEVO
    updateModifierOptionUC) {
        this.createGroupUC = createGroupUC;
        this.updateGroupUC = updateGroupUC;
        this.replaceOptionsUC = replaceOptionsUC;
        this.deleteGroupUC = deleteGroupUC;
        this.listGroupsUC = listGroupsUC;
        this.getGroupUC = getGroupUC;
        this.listByProductUC = listByProductUC;
        this.listProductsByGroupUC = listProductsByGroupUC;
        this.updateModifierOptionUC = updateModifierOptionUC;
        this.createGroup = async (req, res) => {
            try {
                const dto = modifiers_dto_1.CreateModifierGroupDto.parse(req.body);
                const created = await this.createGroupUC.exec(dto);
                return (0, apiResponse_1.success)(res, created, "Created", 201);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error creating modifier group", 400, err);
            }
        };
        this.updateGroup = async (req, res) => {
            try {
                const id = Number(req.params.id);
                const dto = modifiers_dto_2.UpdateModifierGroupDto.parse(req.body);
                const updated = await this.updateGroupUC.exec(id, dto);
                if (dto.replaceOptions) {
                    await this.replaceOptionsUC.exec(id, dto.replaceOptions);
                }
                return (0, apiResponse_1.success)(res, updated, "Updated");
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error updating modifier group", 400, err);
            }
        };
        this.removeGroup = async (req, res) => {
            try {
                const id = Number(req.params.id);
                const hard = req.query.hard === 'true';
                await this.deleteGroupUC.exec(id, hard);
                return (0, apiResponse_1.success)(res, null, hard ? "Deleted" : "Deactivated", 204);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error deleting modifier group", 400, err);
            }
        };
        this.listGroups = async (req, res) => {
            try {
                const q = modifiers_dto_3.ListModifierGroupsQueryDto.parse(req.query);
                const data = await this.listGroupsUC.exec(q);
                return (0, apiResponse_1.success)(res, data);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error listing modifier groups", 400, err);
            }
        };
        this.getGroup = async (req, res) => {
            try {
                const id = Number(req.params.id);
                const mg = await this.getGroupUC.exec(id);
                if (!mg)
                    return (0, apiResponse_1.fail)(res, "Not found", 404);
                return (0, apiResponse_1.success)(res, mg);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error getting modifier group", 400, err);
            }
        };
        this.listByProduct = async (req, res) => {
            try {
                const productId = Number(req.params.productId);
                const data = await this.listByProductUC.exec(productId);
                return (0, apiResponse_1.success)(res, data);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error listing product modifier groups", 400, err);
            }
        };
        this.listProductsByGroup = async (req, res) => {
            try {
                const groupId = Number(req.params.groupId);
                const q = modifiers_dto_4.ListProductsByGroupQueryDto.parse(req.query);
                const data = await this.listProductsByGroupUC.exec(groupId, q);
                return (0, apiResponse_1.success)(res, data);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error listing products by modifier group", 400, err);
            }
        };
        this.updateOption = async (req, res) => {
            try {
                const id = Number(req.params.optionId);
                const body = modifiers_dto_5.UpdateModifierOptionDto.parse(req.body);
                const updated = await this.updateModifierOptionUC.exec(id, body);
                return (0, apiResponse_1.success)(res, updated);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || 'Error updating modifier option', 400, err);
            }
        };
    }
}
exports.ModifiersController = ModifiersController;
//# sourceMappingURL=ModifiersController.js.map