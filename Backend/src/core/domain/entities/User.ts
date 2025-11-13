export interface User {
  id: number;
  email: string;
  name?: string | null;
  roleId: number;
  pinCodeDigest?: string | null;
  //role?: Role;          // opcional si quieres eager load
  createdAt: Date;
  updatedAt: Date;
}
