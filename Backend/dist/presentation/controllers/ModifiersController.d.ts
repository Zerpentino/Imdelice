import { Request, Response } from "express";
import { CreateModifierGroupWithOptions } from "../../core/usecases/modifiers/CreateModifierGroupWithOptions";
import { UpdateModifierGroup } from "../../core/usecases/modifiers/UpdateModifierGroup";
import { ReplaceModifierOptions } from "../../core/usecases/modifiers/ReplaceModifierOptions";
import { DeleteModifierGroup } from "../../core/usecases/modifiers/DeleteModifierGroup";
import { ListModifierGroups } from '../../core/usecases/modifiers/ListModifierGroups';
import { GetModifierGroup } from '../../core/usecases/modifiers/GetModifierGroup';
import { ListModifierGroupsByProduct } from '../../core/usecases/modifiers/ListModifierGroupsByProduct';
import { ListProductsByModifierGroup } from '../../core/usecases/modifiers/ListProductsByModifierGroup';
import { UpdateModifierOption } from '../../core/usecases/modifiers/UpdateModifierOption';
export declare class ModifiersController {
    private createGroupUC;
    private updateGroupUC;
    private replaceOptionsUC;
    private deleteGroupUC;
    private listGroupsUC;
    private getGroupUC;
    private listByProductUC;
    private listProductsByGroupUC;
    private updateModifierOptionUC;
    constructor(createGroupUC: CreateModifierGroupWithOptions, updateGroupUC: UpdateModifierGroup, replaceOptionsUC: ReplaceModifierOptions, deleteGroupUC: DeleteModifierGroup, listGroupsUC: ListModifierGroups, // <—
    getGroupUC: GetModifierGroup, // <—
    listByProductUC: ListModifierGroupsByProduct, listProductsByGroupUC: ListProductsByModifierGroup, // 👈 NUEVO
    updateModifierOptionUC: UpdateModifierOption);
    createGroup: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    updateGroup: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    removeGroup: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    listGroups: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    getGroup: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    listByProduct: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    listProductsByGroup: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
    updateOption: (req: Request, res: Response) => Promise<Response<any, Record<string, any>>>;
}
//# sourceMappingURL=ModifiersController.d.ts.map