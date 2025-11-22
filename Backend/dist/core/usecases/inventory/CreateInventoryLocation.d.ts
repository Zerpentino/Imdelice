import type { IInventoryRepository } from "../../domain/repositories/IInventoryRepository";
import type { InventoryLocationType } from "@prisma/client";
export declare class CreateInventoryLocation {
    private repo;
    constructor(repo: IInventoryRepository);
    exec(input: {
        name: string;
        code?: string | null;
        type?: InventoryLocationType;
        isDefault?: boolean;
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
//# sourceMappingURL=CreateInventoryLocation.d.ts.map