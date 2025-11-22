// src/lib/prisma.ts
import { PrismaClient, Prisma } from '@prisma/client';

const globalForPrisma = global as unknown as { prisma: PrismaClient };

const prismaLogLevels: Prisma.LogLevel[] = ['warn', 'error'];
if (process.env.DEBUG_PRISMA_QUERIES === 'true') {
  prismaLogLevels.unshift('query');
}

export const prisma =
  globalForPrisma.prisma ||
  new PrismaClient({
    log: prismaLogLevels,
  });

if (process.env.NODE_ENV !== 'production') globalForPrisma.prisma = prisma;
