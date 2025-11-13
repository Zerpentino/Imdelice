import type { ICategoryRepository } from '../../core/domain/repositories/ICategoryRepository';
import type { Category } from '@prisma/client';
export declare class PrismaCategoryRepository implements ICategoryRepository {
    create(data: {
        name: string;
        slug: string;
        parentId?: number | null;
        position?: number;
        isComboOnly?: boolean;
    }): Promise<Category>;
    list(): Promise<Category[]>;
    getBySlug(slug: string): Promise<Category | null>;
    update(id: number, data: Partial<Pick<Category, 'name' | 'slug' | 'parentId' | 'position' | 'isActive'>>): Promise<Category>;
    deactivate(id: number): Promise<void>;
    deleteHard(id: number): Promise<void>;
}
//# sourceMappingURL=PrismaCategoryRepository.d.ts.map