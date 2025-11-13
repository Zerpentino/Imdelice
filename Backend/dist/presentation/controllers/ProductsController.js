"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ProductsController = void 0;
const products_dto_1 = require("../dtos/products.dto");
const modifiers_dto_1 = require("../dtos/modifiers.dto");
const apiResponse_1 = require("../utils/apiResponse");
const products_combo_dto_1 = require("../dtos/products.combo.dto");
class ProductsController {
    constructor(createSimpleUC, createVariantedUC, getDetailUC, listUC, attachModUC, updateUC, replaceVariantsUC, deleteUC, convertToVariantedUC, convertToSimpleUC, 
    // COMBOS
    createComboUC, addComboItemsUC, updateComboItemUC, removeComboItemUC, 
    // cambios de grupo de modificadores 
    detachModGroupUC, updateModGroupPosUC, reorderModGroupsUC, 
    // overrides por variante
    attachVariantModUC, updateVariantModUC, detachVariantModUC, listVariantModUC) {
        this.createSimpleUC = createSimpleUC;
        this.createVariantedUC = createVariantedUC;
        this.getDetailUC = getDetailUC;
        this.listUC = listUC;
        this.attachModUC = attachModUC;
        this.updateUC = updateUC;
        this.replaceVariantsUC = replaceVariantsUC;
        this.deleteUC = deleteUC;
        this.convertToVariantedUC = convertToVariantedUC;
        this.convertToSimpleUC = convertToSimpleUC;
        this.createComboUC = createComboUC;
        this.addComboItemsUC = addComboItemsUC;
        this.updateComboItemUC = updateComboItemUC;
        this.removeComboItemUC = removeComboItemUC;
        this.detachModGroupUC = detachModGroupUC;
        this.updateModGroupPosUC = updateModGroupPosUC;
        this.reorderModGroupsUC = reorderModGroupsUC;
        this.attachVariantModUC = attachVariantModUC;
        this.updateVariantModUC = updateVariantModUC;
        this.detachVariantModUC = detachVariantModUC;
        this.listVariantModUC = listVariantModUC;
        this.createSimple = async (req, res) => {
            try {
                const dto = products_dto_1.CreateProductSimpleDto.parse(req.body);
                const image = this.getImagePayload(req, false);
                const prod = await this.createSimpleUC.exec({ ...dto, image });
                return (0, apiResponse_1.success)(res, prod, "Created", 201);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error creating product", 400, err);
            }
        };
        this.createVarianted = async (req, res) => {
            try {
                const dto = products_dto_1.CreateProductVariantedDto.parse(req.body);
                const image = this.getImagePayload(req, false);
                const prod = await this.createVariantedUC.exec({ ...dto, image });
                return (0, apiResponse_1.success)(res, prod, "Created", 201);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error creating product", 400, err);
            }
        };
        this.getDetail = async (req, res) => {
            try {
                const id = Number(req.params.id);
                const prod = await this.getDetailUC.exec(id);
                if (!prod)
                    return (0, apiResponse_1.fail)(res, "Not found", 404);
                return (0, apiResponse_1.success)(res, prod);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error getting product", 400, err);
            }
        };
        this.list = async (req, res) => {
            try {
                const q = products_dto_1.ListProductsQueryDto.parse(req.query);
                const data = await this.listUC.exec(q);
                return (0, apiResponse_1.success)(res, data);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error listing products", 400, err);
            }
        };
        this.attachModifier = async (req, res) => {
            try {
                const dto = modifiers_dto_1.AttachModifierToProductDto.parse(req.body);
                await this.attachModUC.exec(dto);
                return (0, apiResponse_1.success)(res, null, "Attached", 204);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error attaching modifier", 400, err);
            }
        };
        this.listVariantModifierGroups = async (req, res) => {
            try {
                const variantId = Number(req.params.variantId);
                const data = await this.listVariantModUC.exec(variantId);
                return (0, apiResponse_1.success)(res, data);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || 'Error listing variant modifier groups', 400, err);
            }
        };
        this.attachModifierToVariant = async (req, res) => {
            try {
                const variantId = Number(req.params.variantId);
                const dto = products_dto_1.AttachModifierGroupToVariantDto.parse(req.body);
                await this.attachVariantModUC.exec({ variantId, ...dto });
                return (0, apiResponse_1.success)(res, null, 'Attached', 204);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || 'Error attaching modifier to variant', 400, err);
            }
        };
        this.updateVariantModifierGroup = async (req, res) => {
            try {
                const variantId = Number(req.params.variantId);
                const groupId = Number(req.params.groupId);
                const dto = products_dto_1.UpdateVariantModifierGroupDto.parse(req.body);
                await this.updateVariantModUC.exec(variantId, groupId, dto);
                return (0, apiResponse_1.success)(res, null, 'Updated', 204);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || 'Error updating variant modifier group', 400, err);
            }
        };
        this.detachModifierFromVariant = async (req, res) => {
            try {
                const variantId = Number(req.params.variantId);
                const groupId = Number(req.params.groupId);
                await this.detachVariantModUC.exec(variantId, groupId);
                return (0, apiResponse_1.success)(res, null, 'Detached', 204);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || 'Error detaching modifier from variant', 400, err);
            }
        };
        this.update = async (req, res) => {
            try {
                const id = Number(req.params.id);
                const dto = products_dto_1.UpdateProductDto.parse(req.body);
                const image = this.getImagePayload(req, true);
                const prod = await this.updateUC.exec(id, { ...dto, image });
                return (0, apiResponse_1.success)(res, prod, "Updated");
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error updating product", 400, err);
            }
        };
        this.replaceVariants = async (req, res) => {
            try {
                const id = Number(req.params.id);
                const dto = products_dto_1.ReplaceVariantsDto.parse(req.body);
                await this.replaceVariantsUC.exec(id, dto.variants.map(v => ({
                    name: v.name, priceCents: v.priceCents, sku: v.sku
                })));
                return (0, apiResponse_1.success)(res, null, "Variants replaced", 204);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error replacing variants", 400, err);
            }
        };
        this.remove = async (req, res) => {
            try {
                const id = Number(req.params.id);
                const hard = req.query.hard === 'true';
                await this.deleteUC.exec(id, hard);
                return (0, apiResponse_1.success)(res, null, hard ? "Deleted" : "Deactivated", 204);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error deleting product", 400, err);
            }
        };
        this.getImage = async (req, res) => {
            try {
                const id = Number(req.params.id);
                const prod = await this.getDetailUC.exec(id);
                if (!prod || !prod.image || !prod.imageMimeType)
                    return res.status(404).end();
                res.setHeader('Content-Type', prod.imageMimeType);
                res.setHeader('Content-Length', (prod.imageSize ?? prod.image.byteLength).toString());
                return res.status(200).send(Buffer.from(prod.image));
            }
            catch {
                return res.status(404).end();
            }
        };
        this.convertToVarianted = async (req, res) => {
            try {
                const id = Number(req.params.id);
                const dto = products_dto_1.ConvertToVariantedDto.parse(req.body);
                await this.convertToVariantedUC.exec(id, dto.variants);
                return (0, apiResponse_1.success)(res, null, "Converted to VARIANTED");
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error converting product", 400, err);
            }
        };
        this.convertToSimple = async (req, res) => {
            try {
                const id = Number(req.params.id);
                const dto = products_dto_1.ConvertToSimpleDto.parse(req.body);
                await this.convertToSimpleUC.exec(id, dto.priceCents);
                return (0, apiResponse_1.success)(res, null, "Converted to SIMPLE");
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error converting product", 400, err);
            }
        };
        //COMBOS 
        this.createCombo = async (req, res) => {
            try {
                const dto = products_combo_dto_1.CreateComboDto.parse(req.body);
                // Usa tu helper existente:
                const image = this.getImagePayload
                    ? this.getImagePayload(req, false) // nunca null al crear
                    : req.file?.buffer
                        ? { buffer: req.file.buffer, mimeType: req.file.mimetype, size: req.file.size }
                        : undefined;
                const prod = await this.createComboUC.exec({ ...dto, image });
                return (0, apiResponse_1.success)(res, prod, "Created", 201);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error creating combo", 400, err);
            }
        };
        this.addComboItems = async (req, res) => {
            try {
                const productId = Number(req.params.id);
                const dto = products_combo_dto_1.AddComboItemsDto.parse(req.body);
                await this.addComboItemsUC.exec(productId, dto.items);
                return (0, apiResponse_1.success)(res, null, "Items added", 204);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error adding combo items", 400, err);
            }
        };
        this.updateComboItem = async (req, res) => {
            try {
                const comboItemId = Number(req.params.comboItemId);
                const dto = products_combo_dto_1.UpdateComboItemDto.parse(req.body);
                await this.updateComboItemUC.exec(comboItemId, dto);
                return (0, apiResponse_1.success)(res, null, "Item updated", 204);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error updating combo item", 400, err);
            }
        };
        this.removeComboItem = async (req, res) => {
            try {
                const comboItemId = Number(req.params.comboItemId);
                await this.removeComboItemUC.exec(comboItemId);
                return (0, apiResponse_1.success)(res, null, "Item removed", 204);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || "Error removing combo item", 400, err);
            }
        };
        // NUEVOS MÉTODOS PARA GRUPOS DE MODIFICADORES
        this.detachModifierGroup = async (req, res) => {
            try {
                const dto = products_dto_1.DetachModifierGroupDto.parse(req.body);
                await this.detachModGroupUC.exec(dto);
                return (0, apiResponse_1.success)(res, null, 'Link removed', 204);
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || 'Error detaching modifier group', 400, err);
            }
        };
        this.updateModifierGroupPosition = async (req, res) => {
            try {
                const dto = products_dto_1.UpdateModifierGroupPositionDto.parse(req.body);
                await this.updateModGroupPosUC.exec(dto.linkId, dto.position);
                return (0, apiResponse_1.success)(res, null, 'Position updated');
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || 'Error updating position', 400, err);
            }
        };
        this.reorderModifierGroups = async (req, res) => {
            try {
                const dto = products_dto_1.ReorderModifierGroupsDto.parse(req.body);
                await this.reorderModGroupsUC.exec(dto.productId, dto.items);
                return (0, apiResponse_1.success)(res, null, 'Reordered');
            }
            catch (err) {
                return (0, apiResponse_1.fail)(res, err?.message || 'Error reordering groups', 400, err);
            }
        };
    }
    // 👇 IMPLEMENTACIÓN ÚNICA
    getImagePayload(req, allowNull) {
        const file = req.file;
        if (file?.buffer) {
            return { buffer: file.buffer, mimeType: file.mimetype, size: file.size };
        }
        if (allowNull && req.body && Object.prototype.hasOwnProperty.call(req.body, 'image') && !file) {
            return null;
        }
        return undefined;
    }
}
exports.ProductsController = ProductsController;
//# sourceMappingURL=ProductsController.js.map