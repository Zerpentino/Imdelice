// src/core/usecases/roles/GetPermissionsByRole.ts
import { IPermissionRepository } from '../../domain/repositories/IPermissionRepository'
export class GetPermissionsByRole {
  constructor(private repo: IPermissionRepository) {}
  execute(roleId: number) {
    return this.repo.listByRole(roleId)
  }
}
