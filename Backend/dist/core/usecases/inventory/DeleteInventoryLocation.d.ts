import type { IInventoryRepository } from "../../domain/repositories/IInventoryRepository";
export declare class DeleteInventoryLocation {
    private repo;
    constructor(repo: IInventoryRepository);
    exec(id: number): Promise<void>;
}
//# sourceMappingURL=DeleteInventoryLocation.d.ts.map