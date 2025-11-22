import type { IInventoryRepository } from "../../domain/repositories/IInventoryRepository";
export declare class ListInventoryLocations {
    private repo;
    constructor(repo: IInventoryRepository);
    exec(): Promise<{
        name: string;
        id: number;
        createdAt: Date;
        updatedAt: Date;
        code: string | null;
        type: import(".prisma/client").$Enums.InventoryLocationType;
        isActive: boolean;
        isDefault: boolean;
    }[]>;
}
//# sourceMappingURL=ListInventoryLocations.d.ts.map