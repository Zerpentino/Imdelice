import type { IMenuRepository } from '../../domain/repositories/IMenuRepository';
type CreateMenuInput = {
    name: string;
    isActive?: boolean;
    publishedAt?: Date | null;
    version?: number;
};
export declare class CreateMenu {
    private readonly repo;
    constructor(repo: IMenuRepository);
    exec(input: CreateMenuInput): Promise<{
        name: string;
        id: number;
        createdAt: Date;
        updatedAt: Date;
        isActive: boolean;
        deletedAt: Date | null;
        publishedAt: Date | null;
        version: number;
    }>;
}
export {};
//# sourceMappingURL=CreateMenu.d.ts.map