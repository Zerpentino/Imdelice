import type { IModifierRepository } from '../../domain/repositories/IModifierRepository';
export declare class DeleteModifierGroup {
    private repo;
    constructor(repo: IModifierRepository);
    exec(id: number, hard?: boolean): Promise<void>;
}
//# sourceMappingURL=DeleteModifierGroup.d.ts.map