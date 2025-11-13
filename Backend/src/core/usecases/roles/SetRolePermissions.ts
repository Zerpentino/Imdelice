// src/core/usecases/roles/SetRolePermissions.ts
import { IPermissionRepository } from '../../domain/repositories/IPermissionRepository'
export class SetRolePermissions {
  constructor(private repo: IPermissionRepository) {}
  execute(roleId: number, codes: string[]) {
    return this.repo.replaceRolePermissions(roleId, codes)
  }
}

