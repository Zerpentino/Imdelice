import bcrypt from "bcrypt";
import { prisma } from "../../lib/prisma";
import { digestPin } from "../security/pincode";

interface VerifyInput {
  adminEmail?: string;
  adminPin?: string;
  password: string;
}

export class AdminAuthService {
  async verify(input: VerifyInput) {
    if (!input.password) {
      throw new Error("Password requerido");
    }

    let user: any = null;

    if (input.adminEmail) {
      user = await prisma.user.findUnique({
        where: { email: input.adminEmail },
        include: {
          role: {
            include: {
              permissions: {
                include: { permission: true },
              },
            },
          },
        },
      });
    } else if (input.adminPin) {
      const digest = digestPin(input.adminPin);
      user = await prisma.user.findFirst({
        where: { pinCodeDigest: digest },
        include: {
          role: {
            include: {
              permissions: {
                include: { permission: true },
              },
            },
          },
        },
      });
      if (user && user.pinCodeHash) {
        const pinOk = await bcrypt.compare(input.adminPin, user.pinCodeHash);
        if (!pinOk) {
          user = null;
        }
      } else {
        user = null;
      }
    } else {
      throw new Error("Debes enviar adminEmail o adminPin");
    }

    if (!user) {
      const err: any = new Error("Credenciales inválidas");
      err.status = 401;
      throw err;
    }

    const passwordOk = await bcrypt.compare(input.password, user.passwordHash);
    if (!passwordOk) {
      const err: any = new Error("Credenciales inválidas");
      err.status = 401;
      throw err;
    }

    const permissions: string[] = user.role.permissions.map(
      (rp: any) => rp.permission.code
    );
    if (!permissions.includes("orders.refund")) {
      const err: any = new Error("Sin permiso para autorizar cancelaciones");
      err.status = 403;
      throw err;
    }

    return user;
  }
}
