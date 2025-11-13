import { Request, Response } from "express";
import { CreateModifierGroupWithOptions } from "../../core/usecases/modifiers/CreateModifierGroupWithOptions";
import { CreateModifierGroupDto } from "../dtos/modifiers.dto";
import { success, fail } from "../utils/apiResponse";

import { UpdateModifierGroupDto } from "../dtos/modifiers.dto";

import { UpdateModifierGroup } from "../../core/usecases/modifiers/UpdateModifierGroup";
import { ReplaceModifierOptions } from "../../core/usecases/modifiers/ReplaceModifierOptions";
import { DeleteModifierGroup } from "../../core/usecases/modifiers/DeleteModifierGroup";
import { ListModifierGroupsQueryDto } from '../dtos/modifiers.dto';
import { ListModifierGroups } from '../../core/usecases/modifiers/ListModifierGroups';
import { GetModifierGroup } from '../../core/usecases/modifiers/GetModifierGroup';
import { ListModifierGroupsByProduct } from '../../core/usecases/modifiers/ListModifierGroupsByProduct';
import { ListProductsByGroupQueryDto } from '../dtos/modifiers.dto';
import { ListProductsByModifierGroup } from '../../core/usecases/modifiers/ListProductsByModifierGroup';
import { UpdateModifierOption } from '../../core/usecases/modifiers/UpdateModifierOption';
import { UpdateModifierOptionDto } from '../dtos/modifiers.dto';
export class ModifiersController {
  constructor(
    private createGroupUC: CreateModifierGroupWithOptions,
    private updateGroupUC: UpdateModifierGroup,
    private replaceOptionsUC: ReplaceModifierOptions,
    private deleteGroupUC: DeleteModifierGroup,
    private listGroupsUC: ListModifierGroups,                 // <—
  private getGroupUC: GetModifierGroup,                     // <—
  private listByProductUC: ListModifierGroupsByProduct ,
    private listProductsByGroupUC: ListProductsByModifierGroup,   // 👈 NUEVO
    private updateModifierOptionUC: UpdateModifierOption,

) {}

  createGroup = async (req: Request, res: Response) => {
    try {
      const dto = CreateModifierGroupDto.parse(req.body);
      const created = await this.createGroupUC.exec(dto);
      return success(res, created, "Created", 201);
    } catch (err: any) {
      return fail(res, err?.message || "Error creating modifier group", 400, err);
    }
  };
   updateGroup = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      const dto = UpdateModifierGroupDto.parse(req.body);
      const updated = await this.updateGroupUC.exec(id, dto);
      if (dto.replaceOptions) {
        await this.replaceOptionsUC.exec(id, dto.replaceOptions);
      }
      return success(res, updated, "Updated");
    } catch (err: any) { return fail(res, err?.message || "Error updating modifier group", 400, err); }
  };

  removeGroup = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      const hard = req.query.hard === 'true';
      await this.deleteGroupUC.exec(id, hard);
      return success(res, null, hard ? "Deleted" : "Deactivated", 204);
    } catch (err: any) { return fail(res, err?.message || "Error deleting modifier group", 400, err); }
  };
   listGroups = async (req: Request, res: Response) => {
    try {
      const q = ListModifierGroupsQueryDto.parse(req.query);
      const data = await this.listGroupsUC.exec(q);
      return success(res, data);
    } catch (err: any) {
      return fail(res, err?.message || "Error listing modifier groups", 400, err);
    }
  };
getGroup = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.id);
      const mg = await this.getGroupUC.exec(id);
      if (!mg) return fail(res, "Not found", 404);
      return success(res, mg);
    } catch (err: any) {
      return fail(res, err?.message || "Error getting modifier group", 400, err);
    }
  };

  listByProduct = async (req: Request, res: Response) => {
    try {
      const productId = Number(req.params.productId);
      const data = await this.listByProductUC.exec(productId);
      return success(res, data);
    } catch (err: any) {
      return fail(res, err?.message || "Error listing product modifier groups", 400, err);
    }
  };
  listProductsByGroup = async (req: Request, res: Response) => {
  try {
    const groupId = Number(req.params.groupId);
    const q = ListProductsByGroupQueryDto.parse(req.query);
    const data = await this.listProductsByGroupUC.exec(groupId, q);
    return success(res, data);
  } catch (err: any) {
    return fail(res, err?.message || "Error listing products by modifier group", 400, err);
  }
};
updateOption = async (req: Request, res: Response) => {
    try {
      const id = Number(req.params.optionId);
      const body = UpdateModifierOptionDto.parse(req.body);
      const updated = await this.updateModifierOptionUC.exec(id, body);
      return success(res, updated);
    } catch (err: any) {
      return fail(res, err?.message || 'Error updating modifier option', 400, err);
    }
  };
}
