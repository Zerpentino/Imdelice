import type { InventoryLocationType } from "@prisma/client";
import type { IInventoryRepository } from "../../domain/repositories/IInventoryRepository";
export declare class UpdateInventoryLocation {
    private repo;
    constructor(repo: IInventoryRepository);
    exec(id: number, input: {
        name?: string;
        code?: string | null;
        type?: InventoryLocationType;
        isDefault?: boolean;
        isActive?: boolean;
    }): Promise<{
        name: string;
        id: number;
        createdAt: Date;
        updatedAt: Date;
        code: string | null;
        type: import(".prisma/client").$Enums.InventoryLocationType;
        isActive: boolean;
        isDefault: boolean;
    }>;
}
//# sourceMappingURL=UpdateInventoryLocation.d.ts.map