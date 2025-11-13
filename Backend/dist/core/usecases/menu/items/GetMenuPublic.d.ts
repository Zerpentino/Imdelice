import type { IMenuRepository } from '../../../domain/repositories/IMenuRepository';
export declare class GetMenuPublic {
    private readonly repo;
    constructor(repo: IMenuRepository);
    exec(id: number): Promise<import("../../../domain/repositories/IMenuRepository").MenuPublic | null>;
}
//# sourceMappingURL=GetMenuPublic.d.ts.map