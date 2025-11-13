import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';
type CreateSectionInput = {
    menuId: number;
    name: string;
    position?: number;
    categoryId?: number | null;
    isActive?: boolean;
};
export declare class CreateMenuSection {
    private readonly repo;
    constructor(repo: IMenuRepository);
    exec(input: CreateSectionInput): Promise<{
        name: string;
        id: number;
        createdAt: Date;
        updatedAt: Date;
        position: number;
        isActive: boolean;
        categoryId: number | null;
        deletedAt: Date | null;
        menuId: number;
    }>;
}
export {};
//# sourceMappingURL=CreateMenuSection.d.ts.map