import { Request, Response } from "express";
import { CreateProductSimple } from "../../core/usecases/products/CreateProductSimple";
import { CreateProductVarianted } from "../../core/usecases/products/CreateProductVarianted";
import { GetProductDetail } from "../../core/usecases/products/GetProductDetail";
import { ListProducts } from "../../core/usecases/products/ListProducts";
import { AttachModifierGroupToProduct } from "../../core/usecases/products/AttachModifierGroupToProduct";
import {
  CreateProductSimpleDto,
  CreateProductVariantedDto,
  ListProductsQueryDto,
  UpdateProductDto,
  ReplaceVariantsDto,
  ConvertToVariantedDto,
  ConvertToSimpleDto,
  DetachModifierGroupDto,
  UpdateModifierGroupPositionDto,
  ReorderModifierGroupsDto,
  AttachModifierGroupToVariantDto,
  UpdateVariantModifierGroupDto
} from "../dtos/products.dto";
import { AttachModifierToProductDto } from "../dtos/modifiers.dto";
import { success, fail } from "../utils/apiResponse";

import { UpdateProduct } from "../../core/usecases/products/UpdateProduct";
import { ReplaceProductVariants } from "../../core/usecases/products/ReplaceProductVariants";
import { DeleteProduct } from "../../core/usecases/products/DeleteProduct";
// NUEVOS
import { ConvertProductToVarianted } from "../../core/usecases/products/ConvertProductToVarianted";
import { ConvertProductToSimple } from "../../core/usecases/products/ConvertProductToSimple";


//COMBOS :
import { CreateProductCombo } from "../../core/usecases/products/CreateProductCombo";
import { AddComboItems } from "../../core/usecases/products/AddComboItems";
import { UpdateComboItem } from "../../core/usecases/products/UpdateComboItem";
import { RemoveComboItem } from "../../core/usecases/products/RemoveComboItem";
import { CreateComboDto, AddComboItemsDto, UpdateComboItemDto } from "../dtos/products.combo.dto";
import { DetachModifierGroupFromProduct } from '../../core/usecases/products/DetachModifierGroupFromProduct';
import { UpdateModifierGroupPosition } from '../../core/usecases/products/UpdateModifierGroupPosition';
import { ReorderModifierGroups } from '../../core/usecases/products/ReorderModifierGroups';
import { AttachModifierGroupToVariant } from "../../core/usecases/products/AttachModifierGroupToVariant";
import { UpdateVariantModifierGroup } from "../../core/usecases/products/UpdateVariantModifierGroup";
import { DetachModifierGroupFromVariant } from "../../core/usecases/products/DetachModifierGroupFromVariant";
import { ListVariantModifierGroups } from "../../core/usecases/products/ListVariantModifierGroups";


export class ProductsController {
  
  constructor(
    private createSimpleUC: CreateProductSimple,
    private createVariantedUC: CreateProductVarianted,
    private getDetailUC: GetProductDetail,
    private listUC: ListProducts,
    private attachModUC: AttachModifierGroupToProduct,
    private updateUC: UpdateProduct,
    private replaceVariantsUC: ReplaceProductVariants,
    private deleteUC: DeleteProduct,
    private convertToVariantedUC: ConvertProductToVarianted,
    private convertToSimpleUC: ConvertProductToSimple,
    // COMBOS
    private createComboUC: CreateProductCombo,
    private addComboItemsUC: AddComboItems,
    private updateComboItemUC: UpdateComboItem,
    private removeComboItemUC: RemoveComboItem,
    // cambios de grupo de modificadores 
    private detachModGroupUC: DetachModifierGroupFromProduct,
    private updateModGroupPosUC: UpdateModifierGroupPosition,
    private reorderModGroupsUC: ReorderModifierGroups,
    // overrides por variante
    private attachVariantModUC: AttachModifierGroupToVariant,
    private updateVariantModUC: UpdateVariantModifierGroup,
    private detachVariantModUC: DetachModifierGroupFromVariant,
    private listVariantModUC: ListVariantModifierGroups
  ) {}

// 👇 SOBRECARGAS
private getImagePayload(
  req: Request & { file?: Express.Multer.File; body?: any },
  allowNull: false
): { buffer: Buffer; mimeType: string; size: number } | undefined;
private getImagePayload(
  req: Request & { file?: Express.Multer.File; body?: any },
  allowNull: true
): { buffer: Buffer; mimeType: string; size: number } | null | undefined;

// 👇 IMPLEMENTACIÓN ÚNICA
private getImagePayload(
  req: Request & { file?: Express.Multer.File; body?: any },
  allowNull: boolean
) {
  const file = req.file;
  if (file?.buffer) {
    return { buffer: file.buffer, mimeType: file.mimetype, size: file.size };
  }
  if (allowNull && req.body && Object.prototype.hasOwnProperty.call(req.body, 'image') && !file) {
    return null;
  }
  return undefined;
}
  createSimple = async (req: Request, res: Response) => {
    try {
      const dto = CreateProductSimpleDto.parse(req.body);
      const image = this.getImagePayload(req as any, false);
      const prod  = await this.createSimpleUC.exec({ ...dto, image });

      return success(res, prod, "Created", 201);
    } catch (err: any) {
      return fail(res, err?.message || "Error creating product", 400, err);
    }
  };


  createVarianted = async (req: Request, res: Response) => {
    try {
      const dto = CreateProductVariantedDto.parse(req.body);
      const image = this.getImagePayload(req as any, false);
      const prod  = await this.createVariantedUC.exec({ ...dto, image });


      return success(res, prod, "Created", 201);
    } catch (err: any) {
      return fail(res, err?.message || "Error creating product", 400, err);
    }
  };


  getDetail = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      const prod = await this.getDetailUC.exec(id);
      if (!prod) return fail(res, "Not found", 404);
      return success(res, prod);
    } catch (err: any) {
      return fail(res, err?.message || "Error getting product", 400, err);
    }
  };

  list = async (req: Request, res: Response) => {
    try {
      const q = ListProductsQueryDto.parse(req.query);
      const data = await this.listUC.exec(q);
      return success(res, data);
    } catch (err: any) {
      return fail(res, err?.message || "Error listing products", 400, err);
    }
  };

  attachModifier = async (req: Request, res: Response) => {
    try {
      const dto = AttachModifierToProductDto.parse(req.body);
      await this.attachModUC.exec(dto);
      return success(res, null, "Attached", 204);
    } catch (err: any) {
      return fail(res, err?.message || "Error attaching modifier", 400, err);
    }
  };

  listVariantModifierGroups = async (req: Request, res: Response) => {
    try {
      const variantId = Number(req.params.variantId);
      const data = await this.listVariantModUC.exec(variantId);
      return success(res, data);
    } catch (err: any) {
      return fail(res, err?.message || 'Error listing variant modifier groups', 400, err);
    }
  };

  attachModifierToVariant = async (req: Request, res: Response) => {
    try {
      const variantId = Number(req.params.variantId);
      const dto = AttachModifierGroupToVariantDto.parse(req.body);
      await this.attachVariantModUC.exec({ variantId, ...dto });
      return success(res, null, 'Attached', 204);
    } catch (err: any) {
      return fail(res, err?.message || 'Error attaching modifier to variant', 400, err);
    }
  };

  updateVariantModifierGroup = async (req: Request, res: Response) => {
    try {
      const variantId = Number(req.params.variantId);
      const groupId = Number(req.params.groupId);
      const dto = UpdateVariantModifierGroupDto.parse(req.body);
      await this.updateVariantModUC.exec(variantId, groupId, dto);
      return success(res, null, 'Updated', 204);
    } catch (err: any) {
      return fail(res, err?.message || 'Error updating variant modifier group', 400, err);
    }
  };

  detachModifierFromVariant = async (req: Request, res: Response) => {
    try {
      const variantId = Number(req.params.variantId);
      const groupId = Number(req.params.groupId);
      await this.detachVariantModUC.exec(variantId, groupId);
      return success(res, null, 'Detached', 204);
    } catch (err: any) {
      return fail(res, err?.message || 'Error detaching modifier from variant', 400, err);
    }
  };
   update = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      const dto = UpdateProductDto.parse(req.body);
      const image = this.getImagePayload(req as any, true);
      const prod  = await this.updateUC.exec(id, { ...dto, image });
      return success(res, prod, "Updated");
    } catch (err: any) {
      return fail(res, err?.message || "Error updating product", 400, err);
    }
  };

  replaceVariants = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      const dto = ReplaceVariantsDto.parse(req.body);
      await this.replaceVariantsUC.exec(id, dto.variants.map(v => ({
        name: v.name, priceCents: v.priceCents, sku: v.sku
      })));
      return success(res, null, "Variants replaced", 204);
    } catch (err: any) { return fail(res, err?.message || "Error replacing variants", 400, err); }
  };

  remove = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      const hard = req.query.hard === 'true';
      await this.deleteUC.exec(id, hard);
      return success(res, null, hard ? "Deleted" : "Deactivated", 204);
    } catch (err: any) { return fail(res, err?.message || "Error deleting product", 400, err); }
  };

  getImage = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      const prod = await this.getDetailUC.exec(id);
      if (!prod || !prod.image || !prod.imageMimeType) return res.status(404).end();
      res.setHeader('Content-Type', prod.imageMimeType);
      res.setHeader('Content-Length', (prod.imageSize ?? prod.image.byteLength).toString());
      return res.status(200).send(Buffer.from(prod.image as any));
    } catch {
      return res.status(404).end();
    }
  };
  convertToVarianted = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      const dto = ConvertToVariantedDto.parse(req.body);
      await this.convertToVariantedUC.exec(id, dto.variants);
      return success(res, null, "Converted to VARIANTED");
    } catch (err: any) {
      return fail(res, err?.message || "Error converting product", 400, err);
    }
  };

  convertToSimple = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      const dto = ConvertToSimpleDto.parse(req.body);
      await this.convertToSimpleUC.exec(id, dto.priceCents);
      return success(res, null, "Converted to SIMPLE");
    } catch (err: any) {
      return fail(res, err?.message || "Error converting product", 400, err);
    }
  };

  //COMBOS 
 createCombo = async (req: Request, res: Response) => {
  try {
    const dto = CreateComboDto.parse(req.body);
    // Usa tu helper existente:
    const image = (this as any).getImagePayload
      ? (this as any).getImagePayload(req as any, false)  // nunca null al crear
      : (req as any).file?.buffer
        ? { buffer: (req as any).file.buffer, mimeType: (req as any).file.mimetype, size: (req as any).file.size }
        : undefined;

    const prod = await this.createComboUC.exec({ ...dto, image });
    return success(res, prod, "Created", 201);
  } catch (err:any) {
    return fail(res, err?.message || "Error creating combo", 400, err);
  }
};


  addComboItems = async (req: Request, res: Response) => {
  try {
    const productId = Number(req.params.id);
    const dto = AddComboItemsDto.parse(req.body);
    await this.addComboItemsUC.exec(productId, dto.items);
    return success(res, null, "Items added", 204);
  } catch (err:any) { return fail(res, err?.message || "Error adding combo items", 400, err); }
};

updateComboItem = async (req: Request, res: Response) => {
  try {
    const comboItemId = Number(req.params.comboItemId);
    const dto = UpdateComboItemDto.parse(req.body);
    await this.updateComboItemUC.exec(comboItemId, dto);
    return success(res, null, "Item updated", 204);
  } catch (err:any) { return fail(res, err?.message || "Error updating combo item", 400, err); }
};

removeComboItem = async (req: Request, res: Response) => {
  try {
    const comboItemId = Number(req.params.comboItemId);
    await this.removeComboItemUC.exec(comboItemId);
    return success(res, null, "Item removed", 204);
  } catch (err:any) { return fail(res, err?.message || "Error removing combo item", 400, err); }
};



// NUEVOS MÉTODOS PARA GRUPOS DE MODIFICADORES
  detachModifierGroup = async (req: Request, res: Response) => {
    try {
      const dto = DetachModifierGroupDto.parse(req.body);
      await this.detachModGroupUC.exec(dto as any);
      return success(res, null, 'Link removed', 204);
    } catch (err: any) {
      return fail(res, err?.message || 'Error detaching modifier group', 400, err);
    }
  };

  updateModifierGroupPosition = async (req: Request, res: Response) => {
    try {
      const dto = UpdateModifierGroupPositionDto.parse(req.body);
      await this.updateModGroupPosUC.exec(dto.linkId, dto.position);
      return success(res, null, 'Position updated');
    } catch (err: any) {
      return fail(res, err?.message || 'Error updating position', 400, err);
    }
  };

  reorderModifierGroups = async (req: Request, res: Response) => {
    try {
      const dto = ReorderModifierGroupsDto.parse(req.body);
      await this.reorderModGroupsUC.exec(dto.productId, dto.items);
      return success(res, null, 'Reordered');
    } catch (err: any) {
      return fail(res, err?.message || 'Error reordering groups', 400, err);
    }
  };

}
