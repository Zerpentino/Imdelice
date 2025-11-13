// src/core/domain/entities/Permission.ts
export interface Permission {
  id: number
  code: string
  name?: string | null
  description?: string | null
}
