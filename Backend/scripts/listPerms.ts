import { PrismaClient } from '@prisma/client';

async function main() {
  const prisma = new PrismaClient();
  const perms = await prisma.permission.findMany({ select: { code: true } });
  perms.map((p) => p.code).sort().forEach((code) => console.log(code));
  await prisma.$disconnect();
}

main().catch(async (err) => {
  console.error(err);
});
