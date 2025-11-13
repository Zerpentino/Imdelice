import { PrismaClient } from '@prisma/client'
const prisma = new PrismaClient()

const PERMS = [
  'users.read','users.create','users.update','users.delete',
  'roles.read','roles.create','roles.update','roles.delete',
  'categories.read','categories.create','categories.update','categories.delete',
  'modifiers.read','modifiers.create','modifiers.update','modifiers.delete',
  'products.read','products.create','products.update','products.delete',
  'orders.read','orders.create','orders.update','orders.delete','orders.refund',
  'menu.read','menu.create','menu.update','menu.delete','menu.publish',
  'tables.read','tables.create','tables.update','tables.delete'
]

const CHANNEL_CONFIGS: Array<{ source: 'POS' | 'UBER' | 'DIDI' | 'RAPPI'; markupPct: number }> = [
  { source: 'POS', markupPct: 0 },
  { source: 'UBER', markupPct: 46 },
  { source: 'DIDI', markupPct: 40 },
  { source: 'RAPPI', markupPct: 35 },
];

async function main() {
  // Permisos
  for (const code of PERMS) {
    await prisma.permission.upsert({
      where: { code }, update: {},
      create: { code, name: code.toUpperCase() }
    })
  }

  // ADMIN con todos los permisos
  const admin = await prisma.role.upsert({
    where: { name: 'ADMIN' },           // ✅ mismo casing
    update: {},
    create: { name: 'ADMIN', description: 'Acceso total' }
  })
  const allPerms = await prisma.permission.findMany()
  await prisma.rolePermission.deleteMany({ where: { roleId: admin.id }})
  if (allPerms.length) {
    await prisma.rolePermission.createMany({
      data: allPerms.map(p => ({ roleId: admin.id, permissionId: p.id })),
      skipDuplicates: true
    })
  }

  // MESERO con permisos mínimos
  const mesero = await prisma.role.upsert({
    where: { name: 'MESERO' },
    update: {},
    create: { name: 'MESERO', description: 'Operación en piso' }
  })
  const meseroPerms = await prisma.permission.findMany({
    where: { code: { in: ['categories.read', 'modifiers.read', 'menu.read', 'orders.read', 'orders.create', 'orders.update', 'tables.read'] } }
  })
  await prisma.rolePermission.deleteMany({ where: { roleId: mesero.id }})
  if (meseroPerms.length) {
    await prisma.rolePermission.createMany({
      data: meseroPerms.map(p => ({ roleId: mesero.id, permissionId: p.id })),
      skipDuplicates: true
    })
  }

  // Configuración de markup por canal
  for (const cfg of CHANNEL_CONFIGS) {
    await prisma.channelConfig.upsert({
      where: { source: cfg.source },
      update: { markupPct: cfg.markupPct },
      create: { source: cfg.source, markupPct: cfg.markupPct }
    });
  }
}

main().finally(() => prisma.$disconnect())
